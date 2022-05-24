namespace Mycoverse.Net.Json;

public class UploadAvatarRequest : JsonRequest
{
    public string Data { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

public class UploadAvatarResponse : JsonResponse
{
    public UploadAvatarResponse(string msg) : base(false, msg) { }

    public UploadAvatarResponse() : base(true) { }
}