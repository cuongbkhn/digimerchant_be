namespace DigiMerchantBE.Common;

/// <summary>
/// Mã lỗi và mô tả chuẩn cho Auth, User, Role và lỗi hệ thống chung.
/// </summary>
public static class ApiErrorCodes
{
    public static readonly ApiErrorDefinition Success = new()
    {
        Code = "00",
        Description = "Thành công",
        HttpStatusCode = StatusCodes.Status200OK
    };

    public static readonly ApiErrorDefinition InvalidCredentials = new()
    {
        Code = "01",
        Description = "Tên đăng nhập hoặc mật khẩu không đúng",
        HttpStatusCode = StatusCodes.Status401Unauthorized
    };

    public static readonly ApiErrorDefinition AccountInactive = new()
    {
        Code = "02",
        Description = "Tài khoản đã bị vô hiệu hóa",
        HttpStatusCode = StatusCodes.Status403Forbidden
    };

    public static readonly ApiErrorDefinition AccountLocked = new()
    {
        Code = "03",
        Description = "Tài khoản đang bị khóa",
        HttpStatusCode = StatusCodes.Status423Locked
    };

    public static readonly ApiErrorDefinition InvalidRoleAssignment = new()
    {
        Code = "04",
        Description = "Tài khoản chưa được cấp quyền hợp lệ",
        HttpStatusCode = StatusCodes.Status403Forbidden
    };

    public static readonly ApiErrorDefinition RefreshTokenInvalid = new()
    {
        Code = "05",
        Description = "Refresh token không hợp lệ hoặc đã hết hạn",
        HttpStatusCode = StatusCodes.Status401Unauthorized
    };

    public static readonly ApiErrorDefinition UserNotActive = new()
    {
        Code = "06",
        Description = "Người dùng không còn hoạt động",
        HttpStatusCode = StatusCodes.Status401Unauthorized
    };

    public static readonly ApiErrorDefinition Unauthorized = new()
    {
        Code = "07",
        Description = "Chưa xác thực người dùng",
        HttpStatusCode = StatusCodes.Status401Unauthorized
    };

    public static readonly ApiErrorDefinition CurrentUserNotFound = new()
    {
        Code = "08",
        Description = "Không tìm thấy người dùng hiện tại",
        HttpStatusCode = StatusCodes.Status401Unauthorized
    };

    public static readonly ApiErrorDefinition UserNotFound = new()
    {
        Code = "10",
        Description = "Không tìm thấy người dùng",
        HttpStatusCode = StatusCodes.Status404NotFound
    };

    public static readonly ApiErrorDefinition ResetPasswordUserNotFound = new()
    {
        Code = "10",
        Description = "Không tìm thấy người dùng cần reset mật khẩu",
        HttpStatusCode = StatusCodes.Status404NotFound
    };

    public static readonly ApiErrorDefinition RoleHierarchyDenied = new()
    {
        Code = "11",
        Description = "Không đủ quyền theo phân cấp role",
        HttpStatusCode = StatusCodes.Status403Forbidden
    };

    public static readonly ApiErrorDefinition CannotCreateSuperAdmin = new()
    {
        Code = "11",
        Description = "Không được phép tạo tài khoản Super Admin",
        HttpStatusCode = StatusCodes.Status403Forbidden
    };

    public static readonly ApiErrorDefinition CannotResetSuperAdminPassword = new()
    {
        Code = "11",
        Description = "Không được reset mật khẩu Super Admin",
        HttpStatusCode = StatusCodes.Status403Forbidden
    };

    public static readonly ApiErrorDefinition RoleNotFoundOrInactive = new()
    {
        Code = "12",
        Description = "Role không tồn tại hoặc đã bị vô hiệu hóa",
        HttpStatusCode = StatusCodes.Status400BadRequest
    };

    public static readonly ApiErrorDefinition UserRoleNotAssigned = new()
    {
        Code = "12",
        Description = "Người dùng chưa được gán role hợp lệ",
        HttpStatusCode = StatusCodes.Status400BadRequest
    };

    public static readonly ApiErrorDefinition UsernameAlreadyExists = new()
    {
        Code = "13",
        Description = "Tên đăng nhập đã tồn tại",
        HttpStatusCode = StatusCodes.Status409Conflict
    };

    public static readonly ApiErrorDefinition ActorRoleUnknown = new()
    {
        Code = "14",
        Description = "Không xác định được quyền của người dùng hiện tại",
        HttpStatusCode = StatusCodes.Status403Forbidden
    };

    public static readonly ApiErrorDefinition InvalidUsernameFormat = new()
    {
        Code = "15",
        Description = "Tên đăng nhập chỉ được chứa chữ, số và dấu gạch dưới",
        HttpStatusCode = StatusCodes.Status400BadRequest
    };

    public static readonly ApiErrorDefinition CannotResetOwnPassword = new()
    {
        Code = "16",
        Description = "Không được reset mật khẩu của chính mình",
        HttpStatusCode = StatusCodes.Status403Forbidden
    };

    public static readonly ApiErrorDefinition LogoutSuccess = new()
    {
        Code = "00",
        Description = "Đăng xuất thành công",
        HttpStatusCode = StatusCodes.Status200OK
    };

    public static readonly ApiErrorDefinition OttSendSuccess = new()
    {
        Code = "00",
        Description = "Gửi OTT thành công",
        HttpStatusCode = StatusCodes.Status200OK
    };

    public static readonly ApiErrorDefinition CookieSetupFailed = new()
    {
        Code = "99",
        Description = "Không thể thiết lập cookie",
        HttpStatusCode = StatusCodes.Status500InternalServerError
    };

    public static readonly ApiErrorDefinition SystemError = new()
    {
        Code = "99",
        Description = "Có lỗi hệ thống xảy ra",
        HttpStatusCode = StatusCodes.Status500InternalServerError
    };

    public static string FormatRoleHierarchyDenied(string actionLabel, string targetRoleCode) =>
        $"Bạn không có quyền {actionLabel} cho user có role '{targetRoleCode}'";
}
