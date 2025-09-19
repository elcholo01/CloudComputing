using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Web.Security;
using Common;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System.Security.Cryptography;
using System.Text;

namespace MovieDiscussionService.Controllers
{
    public class AccountController : Controller
    {
        private CloudStorageAccount _storageAccount;
        private CloudTableClient _tableClient;
        private CloudTable _usersTable;

        public AccountController()
        {
            InitializeStorage();
        }

        private void InitializeStorage()
        {
            try
            {
                // Koristi connection string iz web.config
                _storageAccount = CloudStorageAccount.Parse(
                    System.Configuration.ConfigurationManager.ConnectionStrings["DataConnectionString"].ConnectionString);
                
                _tableClient = _storageAccount.CreateCloudTableClient();
                _usersTable = _tableClient.GetTableReference("Users");
                
                // Kreiraj tabelu sinhronno
                _usersTable.CreateIfNotExists();
                
                System.Diagnostics.Debug.WriteLine("Azure Storage uspešno inicijalizovan!");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Greška pri inicijalizaciji Azure Storage: {ex.Message}");
                throw new Exception($"Ne mogu da se povežem sa Azure Storage: {ex.Message}");
            }
        }

        [HttpGet]
        public ActionResult Login()
        {
            ViewBag.Title = "Prijava";
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Pokušaj da pronađeš korisnika u Azure Table
                    var retrieveOperation = TableOperation.Retrieve<UserEntity>("User", model.Email.ToLowerInvariant());
                    var result = await _usersTable.ExecuteAsync(retrieveOperation);
                    
                    if (result.Result != null)
                    {
                        var user = result.Result as UserEntity;
                        if (VerifyPassword(model.Password, user.PasswordHash))
                        {
                            FormsAuthentication.SetAuthCookie(model.Email, model.RememberMe);
                            TempData["SuccessMessage"] = $"Dobrodošli {user.FullName}! Uspešno ste se prijavili iz Azure Storage.";
                            return RedirectToAction("Index", "Home");
                        }
                    }
                    
                    ModelState.AddModelError("", "Neispravan email ili lozinka. Korisnik nije pronađen u Azure Storage.");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Greška pri pristupu Azure Storage: {ex.Message}");
                }
            }
            return View(model);
        }

        [HttpGet]
        public ActionResult Register()
        {
            ViewBag.Title = "Registracija";
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var email = model.Email.ToLowerInvariant();
                    
                    // Proveri da li korisnik već postoji u Azure Storage
                    var retrieveOperation = TableOperation.Retrieve<UserEntity>("User", email);
                    var existingResult = await _usersTable.ExecuteAsync(retrieveOperation);
                    
                    if (existingResult.Result != null)
                    {
                        ModelState.AddModelError("", "Korisnik sa ovim email-om već postoji u Azure Storage.");
                        return View(model);
                    }

                    // Kreiraj novog korisnika za Azure Table Storage
                    var user = new UserEntity(email)
                    {
                        FullName = model.FullName,
                        Gender = model.Gender,
                        Country = model.Country,
                        City = model.City,
                        Address = model.Address,
                        PasswordHash = HashPassword(model.Password),
                        PhotoUrl = model.PhotoUrl ?? "",
                        IsAuthorVerified = false
                    };

                    // Sačuvaj u Azure Table Storage
                    var insertOperation = TableOperation.Insert(user);
                    var insertResult = await _usersTable.ExecuteAsync(insertOperation);

                    if (insertResult.HttpStatusCode >= 200 && insertResult.HttpStatusCode < 300)
                    {
                        TempData["SuccessMessage"] = $"Registracija je uspešna! Korisnik {model.FullName} je sačuvan u Azure Storage. Molimo prijavite se.";
                        return RedirectToAction("Login");
                    }
                    else
                    {
                        ModelState.AddModelError("", "Greška prilikom čuvanja u Azure Storage.");
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Greška prilikom registracije u Azure Storage: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"Azure Storage greška: {ex}");
                }
            }
            return View(model);
        }

        [Authorize]
        public ActionResult Logout()
        {
            FormsAuthentication.SignOut();
            TempData["SuccessMessage"] = "Uspešno ste se odjavili!";
            return RedirectToAction("Index", "Home");
        }

        [Authorize]
        public new async Task<ActionResult> Profile()
        {
            ViewBag.Title = "Profil";
            ViewBag.UserEmail = User.Identity.Name;
            
            try
            {
                // Učitaj podatke korisnika iz Azure Storage
                var retrieveOperation = TableOperation.Retrieve<UserEntity>("User", User.Identity.Name.ToLowerInvariant());
                var result = await _usersTable.ExecuteAsync(retrieveOperation);
                
                if (result.Result != null)
                {
                    var user = result.Result as UserEntity;
                    ViewBag.FullName = user.FullName;
                    ViewBag.Country = user.Country;
                    ViewBag.City = user.City;
                    ViewBag.Gender = user.Gender;
                    ViewBag.Address = user.Address;
                    ViewBag.PhotoUrl = user.PhotoUrl;
                    ViewBag.IsAuthorVerified = user.IsAuthorVerified;
                    ViewBag.DataSource = "Azure Storage";
                }
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Greška pri učitavanju profila iz Azure Storage: {ex.Message}";
            }
            
            return View();
        }

        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password + "MovieForum2024"));
                return Convert.ToBase64String(hashedBytes);
            }
        }

        private bool VerifyPassword(string password, string hash)
        {
            var passwordHash = HashPassword(password);
            return passwordHash == hash;
        }

        [Authorize]
        [HttpGet]
        public async Task<ActionResult> EditProfile()
        {
            ViewBag.Title = "Izmena profila";
            
            try
            {
                var retrieveOperation = TableOperation.Retrieve<UserEntity>("User", User.Identity.Name.ToLowerInvariant());
                var result = await _usersTable.ExecuteAsync(retrieveOperation);
                
                if (result.Result != null)
                {
                    var user = result.Result as UserEntity;
                    var model = new EditProfileViewModel
                    {
                        FullName = user.FullName,
                        Gender = user.Gender,
                        Country = user.Country,
                        City = user.City,
                        Address = user.Address,
                        PhotoUrl = user.PhotoUrl
                    };
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Greška pri učitavanju profila: {ex.Message}";
            }
            
            return View(new EditProfileViewModel());
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> EditProfile(EditProfileViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var retrieveOperation = TableOperation.Retrieve<UserEntity>("User", User.Identity.Name.ToLowerInvariant());
                    var result = await _usersTable.ExecuteAsync(retrieveOperation);
                    
                    if (result.Result != null)
                    {
                        var user = result.Result as UserEntity;
                        user.FullName = model.FullName;
                        user.Gender = model.Gender;
                        user.Country = model.Country;
                        user.City = model.City;
                        user.Address = model.Address;
                        user.PhotoUrl = model.PhotoUrl ?? "";

                        var updateOperation = TableOperation.Replace(user);
                        var updateResult = await _usersTable.ExecuteAsync(updateOperation);

                        if (updateResult.HttpStatusCode >= 200 && updateResult.HttpStatusCode < 300)
                        {
                            TempData["SuccessMessage"] = "Profil je uspešno ažuriran!";
                            return RedirectToAction("Profile");
                        }
                        else
                        {
                            ModelState.AddModelError("", "Greška prilikom ažuriranja profila.");
                        }
                    }
                    else
                    {
                        ModelState.AddModelError("", "Korisnik nije pronađen.");
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Greška prilikom ažuriranja profila: {ex.Message}");
                }
            }
            
            return View(model);
        }
    }

    public class LoginViewModel
    {
        [Required(ErrorMessage = "Email je obavezan")]
        [EmailAddress(ErrorMessage = "Neispravan format email adrese")]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Lozinka je obavezna")]
        [DataType(DataType.Password)]
        [Display(Name = "Lozinka")]
        public string Password { get; set; }

        [Display(Name = "Zapamti me")]
        public bool RememberMe { get; set; }
    }

    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Ime i prezime je obavezno")]
        [Display(Name = "Ime i prezime")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Pol je obavezan")]
        [Display(Name = "Pol")]
        public string Gender { get; set; }

        [Required(ErrorMessage = "Država je obavezna")]
        [Display(Name = "Država")]
        public string Country { get; set; }

        [Required(ErrorMessage = "Grad je obavezan")]
        [Display(Name = "Grad")]
        public string City { get; set; }

        [Required(ErrorMessage = "Adresa je obavezna")]
        [Display(Name = "Adresa")]
        public string Address { get; set; }

        [Required(ErrorMessage = "Email je obavezan")]
        [EmailAddress(ErrorMessage = "Neispravan format email adrese")]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Lozinka je obavezna")]
        [StringLength(100, ErrorMessage = "Lozinka mora imati najmanje {2} karaktera.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Lozinka")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Potvrda lozinke")]
        [System.ComponentModel.DataAnnotations.Compare("Password", ErrorMessage = "Lozinka i potvrda lozinke se ne poklapaju.")]
        public string ConfirmPassword { get; set; }

        [Display(Name = "URL slike profila")]
        public string PhotoUrl { get; set; }
    }

    public class EditProfileViewModel
    {
        [Required(ErrorMessage = "Ime i prezime je obavezno")]
        [Display(Name = "Ime i prezime")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Pol je obavezan")]
        [Display(Name = "Pol")]
        public string Gender { get; set; }

        [Required(ErrorMessage = "Država je obavezna")]
        [Display(Name = "Država")]
        public string Country { get; set; }

        [Required(ErrorMessage = "Grad je obavezan")]
        [Display(Name = "Grad")]
        public string City { get; set; }

        [Required(ErrorMessage = "Adresa je obavezna")]
        [Display(Name = "Adresa")]
        public string Address { get; set; }

        [Display(Name = "URL slike profila")]
        public string PhotoUrl { get; set; }
    }
}