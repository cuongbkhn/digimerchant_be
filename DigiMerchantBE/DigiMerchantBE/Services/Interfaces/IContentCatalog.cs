namespace DigiMerchantBE.Services.Interfaces;

public interface IContentCatalog
{
    string BannerDeletedStatus { get; }
    string IconDeletedStatus { get; }

    bool IsBannerWritableStatus(string status);
    bool IsBannerChangeStatus(string status);
    bool IsBannerRenderMode(string renderMode);
    bool TryGetBannerAspectRatio(string aspectRatioCode, out decimal value);

    bool IsIconWritableStatus(string status);
    bool IsIconChangeStatus(string status);

    bool IsPlatform(string platform);
    bool IsActionType(string actionType);

    string ResolveFunctionCodeFilterStatus(string? status);
}
