using Cintix.MockQL.Infrastructure.Domain.Models;

namespace Cintix.MockQL.Application.Services.SqlGenerationManagement;

public interface ISqlGenerator
{
    void GenerateSqlActionMaps(SQLTable table);
}
