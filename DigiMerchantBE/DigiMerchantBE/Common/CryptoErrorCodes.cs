namespace DigiMerchantBE.Common;

/// <summary>
/// Mã lỗi và mô tả cho API mã hóa envelope (CRxxx).
/// </summary>
public static class CryptoErrorCodes
{
    public static readonly ApiErrorDefinition EncryptedPayloadInvalid = Def("CR001", "Payload mã hóa không hợp lệ", StatusCodes.Status400BadRequest);
    public static readonly ApiErrorDefinition EncryptedDataMissing = Def("CR002", "Thiếu dữ liệu mã hóa", StatusCodes.Status400BadRequest);
    public static readonly ApiErrorDefinition AlgorithmNotSupported = Def("CR003", "Thuật toán mã hóa không được hỗ trợ", StatusCodes.Status400BadRequest);
    public static readonly ApiErrorDefinition TimestampInvalid = Def("CR004", "Timestamp không hợp lệ", StatusCodes.Status400BadRequest);
    public static readonly ApiErrorDefinition TimestampExpired = Def("CR005", "Timestamp đã hết hạn", StatusCodes.Status400BadRequest);
    public static readonly ApiErrorDefinition DecryptFailed = Def("CR006", "Payload mã hóa không hợp lệ", StatusCodes.Status400BadRequest);
    public static readonly ApiErrorDefinition PayloadParseFailed = Def("CR007", "Không parse được dữ liệu payload", StatusCodes.Status400BadRequest);
    public static readonly ApiErrorDefinition NonceInvalid = Def("CR008", "Nonce không hợp lệ", StatusCodes.Status400BadRequest);
    public static readonly ApiErrorDefinition NonceReused = Def("CR009", "Nonce đã được sử dụng", StatusCodes.Status409Conflict);
    public static readonly ApiErrorDefinition KidNotFound = Def("CR010", "Không tìm thấy key tương ứng với kid", StatusCodes.Status400BadRequest);
    public static readonly ApiErrorDefinition PrivateKeyPathMissing = Def("CR011", "Private key path chưa được cấu hình", StatusCodes.Status500InternalServerError);
    public static readonly ApiErrorDefinition PrivateKeyNotFound = Def("CR012", "Không tìm thấy private key", StatusCodes.Status500InternalServerError);
    public static readonly ApiErrorDefinition MetadataInvalid = Def("CR013", "Metadata mã hóa không hợp lệ", StatusCodes.Status400BadRequest);
    public static readonly ApiErrorDefinition MetadataLengthInvalid = Def("CR014", "Metadata mã hóa không đúng độ dài", StatusCodes.Status400BadRequest);
    public static readonly ApiErrorDefinition InvalidBase64Field = Def("CR015", "{0} không phải Base64 hợp lệ", StatusCodes.Status400BadRequest);
    public static readonly ApiErrorDefinition AuthTokenInvalid = Def("CR016", "Token xác thực không hợp lệ", StatusCodes.Status401Unauthorized);
    public static readonly ApiErrorDefinition RawPayloadInvalid = Def("CR017", "Payload raw không hợp lệ", StatusCodes.Status400BadRequest);
    public static readonly ApiErrorDefinition EncryptedEnvelopeInvalid = Def("CR018", "Encrypted envelope không hợp lệ", StatusCodes.Status400BadRequest);

    public static string FormatInvalidBase64Field(string fieldName) =>
        string.Format(InvalidBase64Field.Description, fieldName);

    private static ApiErrorDefinition Def(string code, string description, int httpStatusCode) => new()
    {
        Code = code,
        Description = description,
        HttpStatusCode = httpStatusCode
    };
}
