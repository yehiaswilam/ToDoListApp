using Microsoft.AspNetCore.Mvc;

namespace ToDoListApp.Controllers;

public class HomeController : Controller
{
    public IActionResult Index() => View();

    public IActionResult Error() => View();
}
