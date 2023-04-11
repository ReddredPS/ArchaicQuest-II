﻿using ArchaicQuestII.GameLogic.Character.Help;
using ArchaicQuestII.GameLogic.Character.MobFunctions.Healer;
using Microsoft.Extensions.DependencyInjection;

namespace ArchaicQuestII.API.Configuration.IoC.GameLogicExtensions
{
    public static class CharacterExtensions
    {
        public static IServiceCollection AddCharacterLogic(this IServiceCollection services)
        {
            services.AddSingleton<IHealer, Healer>();
            services.AddSingleton<IHelp, HelpFile>();

            return services;
        }
    }
}
