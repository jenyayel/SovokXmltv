
using Newtonsoft.Json;

namespace SovokXmltv.Sovok
{
    public abstract class BaseApiResponse
    {
        public ApiError Error { get; set; }

        public double Servertime { get; set; }
        
        public object Context { get; set; }
    }

    public class ApiError
    {
        public string Message { get; set; }
        public int Code { get; set; }
    }
}
