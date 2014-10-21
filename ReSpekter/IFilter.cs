namespace CyanCor.ReSpekter
{
    public interface IFilter<T>
    {
        bool Check(T subject);
    }
}