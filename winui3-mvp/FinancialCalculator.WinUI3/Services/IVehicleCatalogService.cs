using System.Collections.Generic;
using System.Threading.Tasks;
using FinancialCalculator.WinUI3.Models;

namespace FinancialCalculator.WinUI3.Services;

public interface IVehicleCatalogService
{
    Task LoadAsync();
    IEnumerable<string> GetVehicleClasses();
    IEnumerable<Vehicle> GetVehiclesByClass(string vehicleClass);
    Vehicle? GetClassAverage(string vehicleClass);
    List<string> MbspPackages { get; }
}