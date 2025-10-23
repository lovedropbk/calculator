using System.Threading.Tasks;

namespace FinancialCalculator.Engine.Core;

public interface IRiskParameterRepository
{
    Task LoadAsync(string parametersPath);
    double GetPd(string customerType, string rating);
    (double DcfLgd, double DownturnLgd) GetLgd(string customerType, string assetState, string avc);
    double GetEcTotal();
}