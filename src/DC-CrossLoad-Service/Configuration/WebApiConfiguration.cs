namespace DC_CrossLoad_Service.Configuration
{
    public sealed class WebApiConfiguration
    {
        public WebApiConfiguration(string endPointUrl)
        {
            EndPointUrl = endPointUrl;
        }

        public string EndPointUrl { get; }
    }
}
