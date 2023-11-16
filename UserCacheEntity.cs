namespace FileExchanger;

record UserCacheEntity
{
    public UserCacheEntity(string? Json, UserStatus UserStatus)
    {
        this.Json = Json;
        this.UserStatus = UserStatus;
    }
    public UserCacheEntity(UserStatus UserStatus)
    {
        this.UserStatus = UserStatus;
    }

    public string? Json { get; init; }
    public UserStatus UserStatus { get; set; }

    public void Deconstruct(out string? Json, out UserStatus UserStatus)
    {
        Json = this.Json;
        UserStatus = this.UserStatus;
    }
}