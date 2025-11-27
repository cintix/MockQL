using System;
using Cintix.MockQL.Infrastructure.Domain;
using Cintix.MockQL.Infrastructure.SQLite;

namespace Cintix.MockQL.Infrastructure.Application;

public class MockQlModelService
{
    public ModelDefinition BuildModel(params Type[] types) => ModelConverter.Convert(types);
}
