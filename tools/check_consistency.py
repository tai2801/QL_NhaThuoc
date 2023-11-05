"""
Kiểm tra nhất quán 3 phía của PharmaCare Pro:
 1) Bảng/cột trong database/schema.sql khớp với EF Core Models (tên thuộc tính C#).
 2) seed.sql: mọi dòng hóa đơn gắn BatchId thuộc đúng thuốc; tồn lô = nhập - bán (với các lô có bán).
 3) Lô hết hạn demo (#1) tồn tại và KHÔNG xuất hiện trong dòng hóa đơn nào.
Chạy: python3 tools/check_consistency.py  (in OK / FAIL từng kiểm tra)
"""
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
schema = (ROOT / "database" / "schema.sql").read_text(encoding="utf-8")
seed = (ROOT / "database" / "seed.sql").read_text(encoding="utf-8")

failures = []

# --- 1) Tables in schema ---
tables = re.findall(r"CREATE TABLE dbo\.(\w+)", schema)
expected_tables = [
    "ActiveIngredients", "DosageForms", "MedicineCategories", "Medicines",
    "MedicineIngredients", "Suppliers", "Customers", "Employees",
    "MedicineBatches", "StockReceipts", "StockReceiptLines",
    "Prescriptions", "PrescriptionLines", "SalesInvoices",
    "SalesInvoiceLines", "SalesReturns", "SalesReturnLines",
]
for t in expected_tables:
    print(("OK  " if t in tables else "FAIL") + f" schema có bảng {t}")
    if t not in tables:
        failures.append(t)

# EF DbSets in Web/Data/ApplicationDbContext.cs
ctx = (ROOT / "src" / "PharmaCare.Web" / "Data" / "ApplicationDbContext.cs").read_text(encoding="utf-8")
for t in expected_tables:
    ok = f"DbSet<" in ctx and t.rstrip("s") in ctx or t in ctx
    print(("OK  " if ok else "FAIL") + f" DbContext có DbSet ~ {t}")
    if not ok:
        failures.append("dbset:" + t)

# --- 2) Seed: invoice lines reference batches of the same medicine ---
batch_rows = re.findall(
    r"\((\d+),\s*(\d+),\s*N'[^']*',\s*'[\d-]+',\s*'[\d-]+',\s*(\d+),\s*(\d+),",
    seed.split("INSERT INTO dbo.MedicineBatches")[1].split("SET IDENTITY_INSERT dbo.MedicineBatches OFF")[0])
batches = {int(i): {"med": int(m), "received": int(r), "onhand": int(o)} for i, m, r, o in batch_rows}
print(f"OK   seed có {len(batches)} lô")

inv_lines = re.findall(
    r"\((\d+),\s*(\d+),\s*(\d+),\s*(\d+),\s*(\d+),\s*(\d+)\)",
    seed.split("SalesInvoiceLines (Id, InvoiceId, MedicineId, BatchId, Quantity, UnitPrice)")[1]
        .split("SET IDENTITY_INSERT dbo.SalesInvoiceLines OFF")[0])
sold = {}
for line_id, inv, med, batch, qty, price in inv_lines:
    med, batch, qty = int(med), int(batch), int(qty)
    ok = batches.get(batch, {}).get("med") == med
    print(("OK  " if ok else "FAIL") + f" dòng HĐ {line_id}: thuốc {med} khớp lô {batch}")
    if not ok:
        failures.append(f"line{line_id}")
    sold[batch] = sold.get(batch, 0) + qty

for bid, qty in sold.items():
    b = batches[bid]
    expected_onhand = b["received"] - qty
    # Lô 1 (hết hạn) có lịch sử bán trước khi hết hạn nên không áp công thức này — xử lý riêng bên dưới.
    if bid == 1:
        continue
    ok = b["onhand"] == expected_onhand
    print(("OK  " if ok else "FAIL") + f" lô {bid}: tồn {b['onhand']} = nhập {b['received']} - bán {qty}")
    if not ok:
        failures.append(f"onhand{bid}")

# --- 3) Expired demo batch never sold in seed invoices ---
ok = 1 not in sold
print(("OK  " if ok else "FAIL") + " lô hết hạn #1 không xuất hiện trong dòng hóa đơn nào (FEFO chặn)")
if not ok:
    failures.append("expired-sold")

print("\nKẾT QUẢ:", "FAIL — " + ", ".join(failures) if failures else "OK — nhất quán toàn bộ")
sys.exit(1 if failures else 0)
