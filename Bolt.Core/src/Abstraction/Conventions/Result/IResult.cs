namespace Bolt.Core.Abstraction
{
    public interface IResult
    {
        T GetEntity<T>();
        dynamic GetUnbindValues();
    }
}