using Cintix.MockQL.Infrastructure.Domain.Models;

namespace Cintix.MockQL.Infrastructure.Application.Services.ModelManagement;

public interface IModelConverter
{
    ModelDefinition Convert(params Type[] types);
}
