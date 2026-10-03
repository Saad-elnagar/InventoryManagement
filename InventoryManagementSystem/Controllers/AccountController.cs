using InventoryManagementSystem.DAL.Entities;
using InventoryManagementSystem.Models.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace InventoryManagementSystem.Controllers;

public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly JwtTokenService _jwtTokenService;

    public AccountController(
        UserManager<ApplicationUser> userManager,
        JwtTokenService jwtTokenService)
    {
        _userManager = userManager;
        _jwtTokenService = jwtTokenService;
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(
        LoginViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user =
            await _userManager.FindByEmailAsync(
                model.Email);

        if (user == null)
        {
            ModelState.AddModelError(
                "",
                "Invalid email or password.");

            return View(model);
        }

        var validPassword =
            await _userManager.CheckPasswordAsync(
                user,
                model.Password);

        if (!validPassword)
        {
            ModelState.AddModelError(
                "",
                "Invalid email or password.");

            return View(model);
        }

        var token =
            await _jwtTokenService.CreateTokenAsync(
                user);

        Response.Cookies.Append(
            "access_token",
            token,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,

                Expires =
                    model.RememberMe
                        ? DateTimeOffset.UtcNow.AddDays(7)
                        : DateTimeOffset.UtcNow.AddHours(1)
            });

        return RedirectToAction(
            "Index",
            "Home");
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(
        RegisterViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var exists =
            await _userManager.FindByEmailAsync(
                model.Email);

        if (exists != null)
        {
            ModelState.AddModelError(
                "Email",
                "Email is already registered.");

            return View(model);
        }

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            FullName = model.FullName
        };

        var result =
            await _userManager.CreateAsync(
                user,
                model.Password);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(
                    "",
                    error.Description);
            }

            return View(model);
        }

        var employeeRoleExists =
            await _userManager.IsInRoleAsync(
                user,
                "Employee");

        if (!employeeRoleExists)
        {
            await _userManager.AddToRoleAsync(
                user,
                "Employee");
        }

        var token =
            await _jwtTokenService.CreateTokenAsync(
                user);

        Response.Cookies.Append(
            "access_token",
            token,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,

                Expires =
                    DateTimeOffset.UtcNow.AddHours(1)
            });

        return RedirectToAction(
            "Index",
            "Home");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Logout()
    {
        Response.Cookies.Delete(
            "access_token");

        return RedirectToAction(
            nameof(Login));
    }

    [AllowAnonymous]
    public IActionResult AccessDenied()
    {
        return View();
    }
}