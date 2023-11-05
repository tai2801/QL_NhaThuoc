"""
Test mô hình hóa logic PharmaCare bằng Python (stdlib) — BẢN ĐỐI CHIẾU ĐỘC LẬP của
PharmaCare.Core (FefoAllocator / ExpiryMonitor / ReturnService / ReceiptService).
Mục đích: kiểm chứng quy tắc nghiệp vụ thêm một lần bằng ngôn ngữ khác, độc lập với .NET.
Chạy: python3 -m unittest discover -s tests/python -v
"""
import unittest
from dataclasses import dataclass, field
from datetime import date

TODAY = date(2023, 11, 1)


class InsufficientStock(Exception):
    def __init__(self, available):
        super().__init__(f"Không đủ tồn bán được: còn {available}")
        self.available = available


class InvalidReturnState(Exception):
    pass


@dataclass
class Batch:
    id: int
    medicine_id: int
    batch_number: str
    mfg: date
    expiry: date
    on_hand: int
    status: str = "Active"

    def is_expired(self, today):
        return today > self.expiry

    def is_sellable(self, today):
        return self.status == "Active" and self.on_hand > 0 and not self.is_expired(today)


def fefo_allocate(medicine_id, batches, quantity, today):
    """Mirror FefoAllocator.Allocate — thuần, không sửa dữ liệu."""
    if quantity <= 0:
        raise ValueError("quantity must be > 0")
    cands = sorted(
        [b for b in batches if b.medicine_id == medicine_id and b.is_sellable(today)],
        key=lambda b: (b.expiry, b.mfg, b.id),
    )
    available = sum(b.on_hand for b in cands)
    if available < quantity:
        raise InsufficientStock(available)
    out, remaining = [], quantity
    for b in cands:
        if remaining == 0:
            break
        take = min(remaining, b.on_hand)
        out.append((b, take))
        remaining -= take
    return out


def classify_expiry(expiry, today, critical=30, warning=60, watch=90):
    days = (expiry - today).days
    if days < 0:
        return "Expired"
    if days <= critical:
        return "Critical"
    if days <= warning:
        return "Warning"
    if days <= watch:
        return "Watch"
    return "Safe"


def validate_restock(lines, batch_by_line, today):
    """Mirror ReturnService.ValidateDecision(ApprovedRestock)."""
    for line in lines:
        if not line["intact"]:
            raise InvalidReturnState("Hàng không nguyên vẹn — không hoàn kho")
        batch = batch_by_line[line["invoice_line_id"]]
        if batch.is_expired(today):
            raise InvalidReturnState(f"Lô {batch.batch_number} đã hết hạn — không hoàn kho")


def B(id_, exp, qty, med=1, status="Active", mfg=date(2022, 1, 1)):
    return Batch(id=id_, medicine_id=med, batch_number=f"L{id_}", mfg=mfg,
                 expiry=exp, on_hand=qty, status=status)


class TestFefo(unittest.TestCase):
    def test_earliest_expiry_first(self):
        batches = [B(1, date(2024, 6, 1), 100), B(2, date(2023, 12, 1), 50), B(3, date(2024, 1, 15), 80)]
        alloc = fefo_allocate(1, batches, 30, TODAY)
        self.assertEqual([(2, 30)], [(b.id, q) for b, q in alloc])

    def test_split_across_batches(self):
        batches = [B(1, date(2024, 6, 1), 100), B(2, date(2023, 12, 1), 50), B(3, date(2024, 1, 15), 80)]
        alloc = fefo_allocate(1, batches, 100, TODAY)
        self.assertEqual([(2, 50), (3, 50)], [(b.id, q) for b, q in alloc])

    def test_skips_expired_batch(self):
        batches = [B(1, date(2023, 10, 1), 500), B(2, date(2024, 3, 1), 40)]
        alloc = fefo_allocate(1, batches, 40, TODAY)
        self.assertEqual([(2, 40)], [(b.id, q) for b, q in alloc])

    def test_only_expired_stock_raises_zero_available(self):
        with self.assertRaises(InsufficientStock) as ctx:
            fefo_allocate(1, [B(1, date(2023, 1, 1), 100)], 10, TODAY)
        self.assertEqual(0, ctx.exception.available)

    def test_insufficient_raises_with_available(self):
        batches = [B(1, date(2024, 1, 1), 30), B(2, date(2024, 2, 1), 20)]
        with self.assertRaises(InsufficientStock) as ctx:
            fefo_allocate(1, batches, 51, TODAY)
        self.assertEqual(50, ctx.exception.available)

    def test_skips_quarantined_and_empty(self):
        batches = [B(1, date(2023, 12, 1), 100, status="Quarantined"),
                   B(2, date(2023, 12, 15), 0), B(3, date(2024, 1, 1), 25)]
        alloc = fefo_allocate(1, batches, 25, TODAY)
        self.assertEqual([(3, 25)], [(b.id, q) for b, q in alloc])

    def test_does_not_mutate(self):
        b = B(1, date(2024, 1, 1), 30)
        fefo_allocate(1, [b], 10, TODAY)
        self.assertEqual(30, b.on_hand)


class TestExpiryClassify(unittest.TestCase):
    def test_levels(self):
        cases = {
            date(2023, 10, 31): "Expired",
            date(2023, 12, 1): "Critical",   # 30 ngày
            date(2023, 12, 31): "Warning",   # 60 ngày
            date(2024, 1, 30): "Watch",      # 90 ngày
            date(2024, 3, 1): "Safe",
        }
        for exp, level in cases.items():
            self.assertEqual(level, classify_expiry(exp, TODAY), str(exp))


class TestReturns(unittest.TestCase):
    def test_restock_expired_batch_rejected(self):
        lines = [{"invoice_line_id": 1, "quantity": 5, "intact": True}]
        with self.assertRaises(InvalidReturnState):
            validate_restock(lines, {1: B(9, date(2023, 6, 1), 0)}, TODAY)

    def test_restock_not_intact_rejected(self):
        lines = [{"invoice_line_id": 1, "quantity": 5, "intact": False}]
        with self.assertRaises(InvalidReturnState):
            validate_restock(lines, {1: B(9, date(2024, 6, 1), 0)}, TODAY)

    def test_restock_ok(self):
        lines = [{"invoice_line_id": 1, "quantity": 5, "intact": True}]
        validate_restock(lines, {1: B(9, date(2024, 6, 1), 0)}, TODAY)  # không ném


if __name__ == "__main__":
    unittest.main()
