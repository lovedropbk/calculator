using System.Collections.Generic;
using System.Threading.Tasks;
using FinancialCalculator.WinUI3.Models;

namespace FinancialCalculator.WinUI3.Services
{
    internal sealed class NullVehicleCatalogService : IVehicleCatalogService
    {
        public List<string> MbspPackages { get; } = new();

        public Task LoadAsync() => Task.CompletedTask;

        public IEnumerable<string> GetVehicleClasses() => System.Array.Empty<string>();

        public IEnumerable<Vehicle> GetVehiclesByClass(string vehicleClass)
            => System.Array.Empty<Vehicle>();

        public Vehicle? GetClassAverage(string vehicleClass) => null;
    }
}