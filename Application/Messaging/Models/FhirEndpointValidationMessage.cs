namespace Application.Messaging.Models
{
    public class FhirEndpointValidationMessage
    {
        public Guid EndpointId { get; set; }
        public string BaseUrl { get; set; } = string.Empty;
        public List<string> SupportedResources { get; set; } = [];
        public Guid TestingPatientId { get; set; }
    }
}