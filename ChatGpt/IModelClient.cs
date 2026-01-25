using System.Threading.Tasks;

namespace SmartCar.ChatGpt;

public interface IModelClient
{
    Task SendExecResultAsync(ExecResult result);
}
