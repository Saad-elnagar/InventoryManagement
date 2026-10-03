using InventoryManagementSystem.DAL.Entities;
using InventoryManagementSystem.Models.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagementSystem.Controllers;

[Authorize(Roles = "Admin")]
public class UserController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;

    private static readonly string[] AllowedRoles =
    [
        "Admin",
        "Manager",
        "Employee"
    ];

    public UserController(
        UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var users = await _userManager.Users
            .OrderBy(u => u.FullName)
            .ToListAsync();

        var model = new List<UserManagementViewModel>();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);

            model.Add(new UserManagementViewModel
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email ?? string.Empty,
                Role = roles.FirstOrDefault() ?? "Employee"
            });
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeRole(
        string userId,
        string role)
    {
        if (!AllowedRoles.Contains(role))
            return BadRequest();

        var user = await _userManager.FindByIdAsync(userId);

        if (user == null)
            return NotFound();

        if (user.Id == _userManager.GetUserId(User) &&
            role != "Admin")
        {
            TempData["Error"] =
                "You cannot remove your own Admin role.";

            return RedirectToAction(nameof(Index));
        }

        var currentRoles =
            await _userManager.GetRolesAsync(user);

        if (currentRoles.Count > 0)
        {
            var removeResult =
                await _userManager.RemoveFromRolesAsync(
                    user,
                    currentRoles);

            if (!removeResult.Succeeded)
            {
                foreach (var error in removeResult.Errors)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        error.Description);
                }

                return RedirectToAction(nameof(Index));
            }
        }

        var addResult =
            await _userManager.AddToRoleAsync(
                user,
                role);

        if (!addResult.Succeeded)
        {
            foreach (var error in addResult.Errors)
            {
                ModelState.AddModelError(
                    string.Empty,
                    error.Description);
            }

            return RedirectToAction(nameof(Index));
        }

        TempData["Success"] =
            $"{user.FullName} is now {role}.";

        return RedirectToAction(nameof(Index));
    }
}