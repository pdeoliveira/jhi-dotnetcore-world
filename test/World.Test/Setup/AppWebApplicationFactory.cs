using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using company.world.Crosscutting.Constants;
using company.world.Configuration;
using company.world.Security;
using company.world.Test.Configuration;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.JsonWebTokens;

namespace company.world.Test.Setup
{
    public class AppWebApplicationFactory<TEntryPoint> : WebApplicationFactory<TEntryPoint>
        where TEntryPoint : class
    {
        private IServiceProvider _serviceProvider;
        private ClaimsPrincipal _user { get; set; }

        protected override IWebHostBuilder CreateWebHostBuilder()
        {
            return WebHost.CreateDefaultBuilder()
                          .UseStartup<TEntryPoint>();
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder
                .UseSolutionRelativeContentRoot("src/World")
                .ConfigureServices(services =>
                {
                    services
                        .AddMvc(TestMvcStartup.ConfigureMvcAuthorization());
                    services.Replace(new ServiceDescriptor(typeof(IHttpContextFactory), typeof(MockHttpContextFactory),
                        ServiceLifetime.Transient));
                    services.AddTransient(sp => new MockClaimsPrincipalProvider(_user));
                });
        }

        public TService GetRequiredService<TService>()
        {
            if (_serviceProvider == null) _serviceProvider = Server.Host.Services.CreateScope().ServiceProvider;

            return _serviceProvider.GetRequiredService<TService>();
        }

        public AppWebApplicationFactory<TEntryPoint> WithMockUser(string name = "user",
            IEnumerable<string> roles = null, string authenticationType = "MockAuthenticationType")
        {
            _user = BuildClaimsPrincipal(name, roles, authenticationType);
            return this;
        }

        private static ClaimsPrincipal BuildClaimsPrincipal(string name, IEnumerable<string> roles,
            string authenticationType)
        {
            if (roles == null || !roles.Any()) roles = new HashSet<string> { RolesConstants.USER };

            var claims = new List<Claim> { new Claim(SecurityStartup.UserNameClaimType, name) };
            claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
            return new ClaimsPrincipal(new ClaimsIdentity(claims.ToArray(), authenticationType));
        }

    }
}
