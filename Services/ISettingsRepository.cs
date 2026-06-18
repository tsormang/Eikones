using Eikones.Models;

namespace Eikones.Services;

public interface ISettingsRepository
{
    AppSettings Load();
    void Save(AppSettings settings);
}
