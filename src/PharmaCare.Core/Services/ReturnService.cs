using PharmaCare.Core.Exceptions;
using PharmaCare.Core.Models;

namespace PharmaCare.Core.Services;

/// <summary>
/// Quy tắc xử lý trả thuốc:
///  - Phiếu trả bắt buộc có lý do, số lượng trả không vượt số đã bán ở đúng dòng hóa đơn đó.
///  - Chỉ dược sĩ / admin mới duyệt (kiểm tra ở tầng Web bằng [Authorize(Roles)] + kiểm tra lại ở đây theo Role).
///  - Hoàn kho (ApprovedRestock) CHỈ khi: mọi dòng còn nguyên vẹn VÀ lô nguồn chưa hết hạn tại ngày duyệt.
///    Nếu lô đã hết hạn hoặc hàng không nguyên vẹn → chỉ được duyệt tiêu hủy (ApprovedDestroy) hoặc từ chối.
///  - Khi hoàn kho: cộng lại đúng lô nguồn (truy xuất 2 chiều nhờ InvoiceLine.BatchId).
/// </summary>
public static class ReturnService
{
    public static void ValidateCreate(SalesReturn ret, IReadOnlyDictionary<int, int> soldQtyByInvoiceLine)
    {
        if (string.IsNullOrWhiteSpace(ret.Reason))
            throw new InvalidReturnStateException("Phiếu trả bắt buộc ghi lý do trả thuốc.");
        if (ret.Lines.Count == 0)
            throw new InvalidReturnStateException("Phiếu trả phải có ít nhất một dòng.");
        foreach (var line in ret.Lines)
        {
            if (line.Quantity <= 0)
                throw new InvalidReturnStateException("Số lượng trả phải lớn hơn 0.");
            if (!soldQtyByInvoiceLine.TryGetValue(line.InvoiceLineId, out var sold) || line.Quantity > sold)
                throw new InvalidReturnStateException(
                    $"Số lượng trả của dòng hóa đơn #{line.InvoiceLineId} vượt số đã bán.");
        }
    }

    /// <summary>Quyết định duyệt có hợp lệ với tình trạng hàng/lô hay không (không tự sửa dữ liệu).</summary>
    public static void ValidateDecision(
        SalesReturn ret, ReturnStatus decision, UserRole reviewerRole,
        IReadOnlyDictionary<int, MedicineBatch> batchByInvoiceLine, DateOnly today)
    {
        if (ret.Status != ReturnStatus.Pending)
            throw new InvalidReturnStateException("Phiếu trả đã được xử lý, không duyệt lại.");
        if (reviewerRole == UserRole.Staff)
            throw new InvalidReturnStateException("Nhân viên không có quyền duyệt phiếu trả — cần dược sĩ hoặc quản trị.");

        if (decision == ReturnStatus.ApprovedRestock)
        {
            foreach (var line in ret.Lines)
            {
                if (!line.IsIntact)
                    throw new InvalidReturnStateException("Hàng không còn nguyên vẹn — không được hoàn kho, chỉ tiêu hủy hoặc từ chối.");
                var batch = batchByInvoiceLine[line.InvoiceLineId];
                if (batch.IsExpired(today))
                    throw new InvalidReturnStateException($"Lô {batch.BatchNumber} đã hết hạn — không được hoàn kho.");
            }
        }
    }

    /// <summary>Áp dụng hoàn kho sau khi ValidateDecision(ApprovedRestock) đạt.</summary>
    public static void ApplyRestock(SalesReturn ret, IReadOnlyDictionary<int, MedicineBatch> batchByInvoiceLine)
    {
        foreach (var line in ret.Lines)
        {
            var batch = batchByInvoiceLine[line.InvoiceLineId];
            batch.QuantityOnHand += line.Quantity;
            if (batch.Status == BatchStatus.Depleted) batch.Status = BatchStatus.Active;
        }
    }
}
