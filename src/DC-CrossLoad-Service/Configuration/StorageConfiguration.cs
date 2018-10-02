using ESFA.DC.IO.AzureStorage.Config.Interfaces;

namespace DC_CrossLoad_Service.Configuration
{
    public sealed class StorageConfiguration : IAzureStorageKeyValuePersistenceServiceConfig
    {
        public StorageConfiguration(string connectionString, string containerName)
        {
            ConnectionString = connectionString;
            ContainerName = containerName;
        }

        public string ConnectionString { get; }

        public string ContainerName { get; }
    }
}
