using Dsw2026Tpi.CrossCutting.Identity;
using Dsw2026Tpi.Data.Identity;
using Microsoft.AspNetCore.Identity;

namespace Dsw2026Tpi.Api.Configurations
{
    public static class AdminInitializer
    {
        public static async Task InitializeAdminAsync(
            IServiceProvider serviceProvider,
            IConfiguration configuration)
        {
            using var scope = serviceProvider.CreateScope();

			var userManager =
				scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

			var roleManager =
				scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

            if (!await roleManager.RoleExistsAsync(Roles.Administrator))
            {
                var createRoleResult = await roleManager.CreateAsync(new IdentityRole<Guid>(Roles.Administrator));

                if (!createRoleResult.Succeeded)
                {
                    var errors = string.Join("; ", createRoleResult.Errors.Select(e => e.Description));
                    throw new InvalidOperationException($"No se pudo crear el rol Administrador: {errors}");
                }
            }

            var adminEmail = configuration["Admin:Email"]
				?? throw new InvalidOperationException(
					"No se configuró Admin:Email.");

			var adminPassword = configuration["Admin:Password"]
				?? throw new InvalidOperationException(
					"No se configuró Admin:Password.");

			var admin = await userManager.FindByEmailAsync(adminEmail);

			if (admin is not null)
			{
				return;
			}

            var now = DateTime.UtcNow;

            admin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                Deleted = false,
                CreatedAt = now,
                UpdatedAt = now
            };

			var creationResult =
				await userManager.CreateAsync(
					admin, 
					adminPassword);

			if (!creationResult.Succeeded)
			{
				var errors = string.Join(
					"; ",
					creationResult.Errors.Select(error => error.Description));

				throw new InvalidOperationException(
					$"No se pudo crear el usuario administrador: {errors}");
			}

			var roleResult =
				await userManager.AddToRoleAsync(
					admin,
					Roles.Administrator);

			if (!roleResult.Succeeded)
			{
				var errors = string.Join(
					"; ",
					roleResult.Errors.Select(error => error.Description));

				throw new InvalidOperationException(
					$"No se pudo asignar el rol Administrador: {errors}");
			}
		}
	}
}
