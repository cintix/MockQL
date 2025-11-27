using Cintix.MockQL.Infrastructure.Domain.Models;

namespace Cintix.MockQL.Infrastructure.Application.Services.ModelManagement;

public interface IModelWriter
{
    void Build(ModelDefinition model, string @namespace, string path);
}
