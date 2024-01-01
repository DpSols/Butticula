namespace FileExchanger;

internal enum UserStatus
{
    WaitingForJsonFilename,
    WaitingForXmlFilename,
    JsonPrettified,
    XmlPrettified,
    FileNamed
}