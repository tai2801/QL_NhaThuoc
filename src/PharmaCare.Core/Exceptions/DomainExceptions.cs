namespace PharmaCare.Core.Exceptions;

/// <summary>Không đủ tồn ở các lô còn bán được (đã loại lô hết hạn / cách ly / hết hàng).</summary>
public class InsufficientStockException : Exception
{
    public int MedicineId { get; }
    public int Requested { get; }
    public int Available { get; }

    public InsufficientStockException(int medicineId, int requested, int available)
        : base($"Thuốc #{medicineId}: yêu cầu {requested} nhưng chỉ còn {available} ở các lô còn bán được.")
    {
        MedicineId = medicineId;
        Requested = requested;
        Available = available;
    }
}

/// <summary>Cố bán một lô đã hết hạn — bị chặn tuyệt đối.</summary>
public class ExpiredBatchException : Exception
{
    public ExpiredBatchException(string batchNumber, DateOnly expiry)
        : base($"Lô {batchNumber} đã hết hạn từ {expiry:dd/MM/yyyy} — không được bán.") { }
}

/// <summary>Thuốc kê đơn nhưng hóa đơn không gắn đơn thuốc hợp lệ.</summary>
public class PrescriptionRequiredException : Exception
{
    public PrescriptionRequiredException(string medicineName)
        : base($"Thuốc '{medicineName}' là thuốc kê đơn — cần gắn đơn thuốc còn hiệu lực.") { }
}

/// <summary>Thao tác không hợp lệ theo trạng thái hiện tại (vd duyệt phiếu trả đã xử lý).</summary>
public class InvalidReturnStateException : Exception
{
    public InvalidReturnStateException(string message) : base(message) { }
}
