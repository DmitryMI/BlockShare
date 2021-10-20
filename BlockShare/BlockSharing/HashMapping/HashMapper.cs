namespace BlockShare.BlockSharing.HashMapping
{
    public abstract class HashMapper
    {
        public abstract string GetHashpartFile(string filePath);
        public abstract string GetHashlistFile(string filePath);
    }
}
