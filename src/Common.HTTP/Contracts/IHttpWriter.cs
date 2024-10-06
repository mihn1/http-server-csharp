namespace Common.HTTP.Contracts
{
    public interface IHttpWriter
    {
        void WriteAll(Stream stream, HttpResponseMessage message);
    }
}
