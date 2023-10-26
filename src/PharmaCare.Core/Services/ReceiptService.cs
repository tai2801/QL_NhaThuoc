using PharmaCare.Core.Models;

namespace PharmaCare.Core.Services;

/// <summary>
/// Quy tắc nhập kho: MỖI dòng nhập tạo ĐÚNG MỘT lô mới.
///  - Số lô không trùng trong cùng một thuốc (trùng thì phải là lần nhập bổ sung có chủ đích → vẫn tạo lô mới
///    với cùng BatchNumber? KHÔNG: PharmaCare bắt buộc tách lô theo lần nhập để giá nhập/HSD độc lập;
///    nếu NCC giao cùng số lô + cùng HSD + cùng giá nhập, gộp vào phiếu nhập một dòng duy nhất.)
///  - HSD phải sau NSX ít nhất 30 ngày và sau ngày nhập (không nhận hàng đã hết hạn).
///  - Số lượng và giá nhập phải dương.
/// </summary>
public static class ReceiptService
{
    public static MedicineBatch CreateBatchForLine(
        StockReceipt receipt, StockReceiptLine line,
        string batchNumber, DateOnly manufactureDate, DateOnly expiryDate, DateOnly today)
    {
        if (line.Quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(line), "Số lượng nhập phải lớn hơn 0.");
        if (line.ImportPrice <= 0)
            throw new ArgumentOutOfRangeException(nameof(line), "Giá nhập phải lớn hơn 0.");
        if (expiryDate <= manufactureDate)
            throw new InvalidOperationException("Hạn sử dụng phải sau ngày sản xuất.");
        if (expiryDate <= today)
            throw new InvalidOperationException($"Không nhận hàng đã hết hạn (HSD {expiryDate:dd/MM/yyyy}).");
        if ((expiryDate.DayNumber - manufactureDate.DayNumber) < 30)
            throw new InvalidOperationException("HSD phải cách NSX ít nhất 30 ngày — kiểm tra lại dữ liệu lô.");

        return new MedicineBatch
        {
            MedicineId = line.MedicineId,
            BatchNumber = batchNumber,
            ManufactureDate = manufactureDate,
            ExpiryDate = expiryDate,
            QuantityReceived = line.Quantity,
            QuantityOnHand = line.Quantity,
            ImportPrice = line.ImportPrice,
            Status = BatchStatus.Active,
            CreatedAt = DateTime.Now
        };
    }
}
