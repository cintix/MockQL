using System;
using Cintix.MockQL.Infrastructure.Application.Services.ModelManagement;
using Cintix.MockQL.Infrastructure.Domain;
using Cintix.MockQL.Infrastructure.Domain.Models;
using Cintix.MockQL.Infrastructure.SQLite;

namespace Cintix.MockQL.Infrastructure.Application;

public class MockQLService
{
    private readonly IModelConverter _modelConverter = new ModelConverter();
    public ModelDefinition BuildModel(params Type[] types) => _modelConverter.Convert(types);
}
