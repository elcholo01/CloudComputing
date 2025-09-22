using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Common;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.WindowsAzure.Storage.Queue;
using MovieDiscussionService.Models;

namespace MovieDiscussionService.Controllers
{
    public class DiscussionController : Controller
    {
        private CloudStorageAccount _storageAccount;
        private CloudTableClient _tableClient;
        private CloudTable _discussionsTable;
        private CloudQueueClient _queueClient;
        private CloudQueue _notificationsQueue;

        public DiscussionController()
        {
            InitializeStorage();
        }

        private void InitializeStorage()
        {
            try
            {
                _storageAccount = CloudStorageAccount.Parse(
                    System.Configuration.ConfigurationManager.ConnectionStrings["DataConnectionString"].ConnectionString);
                
                _tableClient = _storageAccount.CreateCloudTableClient();
                _discussionsTable = _tableClient.GetTableReference("Discussions");
                
                _queueClient = _storageAccount.CreateCloudQueueClient();
                _notificationsQueue = _queueClient.GetQueueReference("notifications");
                
                // Kreiraj tabele i queue-ove ako ne postoje
                _discussionsTable.CreateIfNotExists();
                _notificationsQueue.CreateIfNotExists();
                
                System.Diagnostics.Debug.WriteLine("Discussion Azure Storage uspešno inicijalizovan!");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Greška pri inicijalizaciji Discussion Azure Storage: {ex.Message}");
                // Ne bacaj exception, koristi fallback
                _discussionsTable = null;
            }
        }

        // GET: Discussion
        public async Task<ActionResult> Index(string searchTerm = "", string sortBy = "date", string genre = "", int page = 1, int pageSize = 10)
        {
            ViewBag.Title = "Diskusije o filmovima";
            ViewBag.Message = "Lista diskusija o filmovima";
            ViewBag.SearchTerm = searchTerm;
            ViewBag.SortBy = sortBy;
            ViewBag.Genre = genre;
            
            try
            {
                if (_discussionsTable != null)
                {
                    // Učitaj diskusije iz Azure Storage
                    var query = new TableQuery<DiscussionEntity>()
                        .Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "Disc"));
                    
                    var discussions = new List<DiscussionEntity>();
                    TableContinuationToken token = null;
                    
                    do
                    {
                        var segment = await _discussionsTable.ExecuteQuerySegmentedAsync(query, token);
                        token = segment.ContinuationToken;
                        discussions.AddRange(segment.Results);
                    } while (token != null);
                    
                    // Filtriraj po pretrazi
                    if (!string.IsNullOrEmpty(searchTerm))
                    {
                        discussions = discussions.Where(d => 
                            d.MovieTitle.ToLower().Contains(searchTerm.ToLower()) ||
                            d.Genre.ToLower().Contains(searchTerm.ToLower()) ||
                            d.Synopsis.ToLower().Contains(searchTerm.ToLower())
                        ).ToList();
                    }
                    
                    // Filtriraj po žanru
                    if (!string.IsNullOrEmpty(genre))
                    {
                        discussions = discussions.Where(d => 
                            d.Genre.ToLower().Contains(genre.ToLower())
                        ).ToList();
                    }
                    
                    // Sortiraj
                    switch (sortBy.ToLower())
                    {
                        case "title":
                            discussions = discussions.OrderBy(d => d.MovieTitle).ToList();
                            break;
                        case "rating":
                            discussions = discussions.OrderByDescending(d => d.ImdbRating).ToList();
                            break;
                        case "year":
                            discussions = discussions.OrderByDescending(d => d.Year).ToList();
                            break;
                        case "positive":
                            discussions = discussions.OrderByDescending(d => d.Positive).ToList();
                            break;
                        case "negative":
                            discussions = discussions.OrderByDescending(d => d.Negative).ToList();
                            break;
                        default: // date
                            discussions = discussions.OrderByDescending(d => d.CreatedUtc).ToList();
                            break;
                    }
                    
                    var allDiscussions = discussions.Select(disc => new DiscussionViewModel
                    {
                        Title = disc.MovieTitle,
                        Author = disc.CreatorEmail,
                        Date = disc.CreatedUtc.ToString("dd.MM.yyyy"),
                        Id = disc.RowKey,
                        Genre = disc.Genre,
                        Year = disc.Year,
                        ImdbRating = disc.ImdbRating,
                        Positive = disc.Positive,
                        Negative = disc.Negative,
                        Synopsis = disc.Synopsis,
                        CreatorEmail = disc.CreatorEmail
                    }).ToList();
                    
                    // Paginacija
                    var totalItems = allDiscussions.Count;
                    var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);
                    var pagedDiscussions = allDiscussions
                        .Skip((page - 1) * pageSize)
                        .Take(pageSize)
                        .ToList();
                    
                    ViewBag.Discussions = pagedDiscussions;
                    ViewBag.DataSource = "Azure Storage";
                    ViewBag.CurrentPage = page;
                    ViewBag.PageSize = pageSize;
                    ViewBag.TotalPages = totalPages;
                    ViewBag.TotalItems = totalItems;
                }
                else
                {
                    // Fallback - simulirane diskusije
                    var allDiscussions = new List<DiscussionViewModel>
                    {
                        new DiscussionViewModel { Title = "Najbolji filmovi 2024", Author = "marko@example.com", Date = "01.09.2024", Id = "1", Genre = "Drama", Year = 2024, ImdbRating = 8.5, Positive = 15, Negative = 2, CreatorEmail = "marko@example.com" },
                        new DiscussionViewModel { Title = "Komentari o filmu Oppenheimer", Author = "ana@example.com", Date = "31.08.2024", Id = "2", Genre = "Drama", Year = 2023, ImdbRating = 8.8, Positive = 25, Negative = 1, CreatorEmail = "ana@example.com" },
                        new DiscussionViewModel { Title = "Preporuke za horor filmove", Author = "petar@example.com", Date = "30.08.2024", Id = "3", Genre = "Horor", Year = 2024, ImdbRating = 7.2, Positive = 8, Negative = 5, CreatorEmail = "petar@example.com" }
                    };
                    
                    // Paginacija za fallback
                    var totalItems = allDiscussions.Count;
                    var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);
                    var pagedDiscussions = allDiscussions
                        .Skip((page - 1) * pageSize)
                        .Take(pageSize)
                        .ToList();
                    
                    ViewBag.Discussions = pagedDiscussions;
                    ViewBag.DataSource = "In-Memory (Fallback)";
                    ViewBag.CurrentPage = page;
                    ViewBag.PageSize = pageSize;
                    ViewBag.TotalPages = totalPages;
                    ViewBag.TotalItems = totalItems;
                }
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Greška pri učitavanju diskusija: {ex.Message}";
                ViewBag.Discussions = new List<DiscussionViewModel>();
            }
            
            return View();
        }

        [Authorize]
        public async Task<ActionResult> Create()
        {
            ViewBag.Title = "Kreiranje nove diskusije";
            
            // Proveri da li je korisnik verifikovan autor
            var isAuthorVerified = await IsUserVerifiedAuthor(User.Identity.Name);
            
            if (!isAuthorVerified)
            {
                TempData["ErrorMessage"] = "Samo verifikovani autori mogu da kreiraju diskusije. Molimo kontaktirajte administratora da vas verifikuje.";
                return RedirectToAction("Index");
            }
            
            return View();
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(CreateDiscussionViewModel model)
        {
            // Proveri da li je korisnik verifikovan autor
            var isAuthorVerified = await IsUserVerifiedAuthor(User.Identity.Name);
            
            if (!isAuthorVerified)
            {
                TempData["ErrorMessage"] = "Samo verifikovani autori mogu da kreiraju diskusije. Molimo kontaktirajte administratora da vas verifikuje.";
                return RedirectToAction("Index");
            }
            
            if (ModelState.IsValid)
            {
                try
                {
                    if (_discussionsTable != null)
                    {
                        // Sačuvaj u Azure Storage
                        var discussionId = Guid.NewGuid().ToString();
                        var discussion = new DiscussionEntity(discussionId)
                        {
                            MovieTitle = model.Title,
                            Synopsis = model.Content,
                            Genre = model.Genre ?? "Ostalo",
                            Year = model.Year ?? 2024,
                            ImdbRating = (double)(model.ImdbRating ?? 0),
                            DurationMin = model.Duration ?? 0,
                            PosterUrl = model.ImageUrl ?? "",
                            CreatorEmail = User.Identity.Name,
                            CreatedUtc = DateTime.UtcNow,
                            Positive = 0,
                            Negative = 0
                        };

                        var insertOperation = TableOperation.Insert(discussion);
                        var result = await _discussionsTable.ExecuteAsync(insertOperation);

                        if (result.HttpStatusCode >= 200 && result.HttpStatusCode < 300)
                        {
                            TempData["SuccessMessage"] = $"Diskusija '{model.Title}' je uspešno kreirana i sačuvana u Azure Storage!";
                            System.Diagnostics.Debug.WriteLine($"Diskusija uspešno sačuvana sa ID: {discussionId}");
                        }
                        else
                        {
                            TempData["ErrorMessage"] = "Greška pri čuvanju u Azure Storage.";
                            System.Diagnostics.Debug.WriteLine($"Greška pri čuvanju: HTTP {result.HttpStatusCode}");
                        }
                    }
                    else
                    {
                        // Fallback mode
                        TempData["SuccessMessage"] = $"Diskusija '{model.Title}' je kreirana (Test mode - Azure Storage nedostupan)!";
                    }
                    
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Greška pri kreiranju diskusije: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"Discussion Create Error: {ex}");
                }
            }
            
            ViewBag.Title = "Kreiranje nove diskusije";
            return View(model);
        }

        public async Task<ActionResult> Details(string id = "1")
        {
            ViewBag.Title = "Detalji diskusije";
            
            try
            {
                if (_discussionsTable != null && id != "1")
                {
                    // Učitaj iz Azure Storage
                    var retrieveOperation = TableOperation.Retrieve<DiscussionEntity>("Disc", id);
                    var result = await _discussionsTable.ExecuteAsync(retrieveOperation);
                    
                    if (result.Result != null)
                    {
                        var discussion = result.Result as DiscussionEntity;
                        ViewBag.DiscussionId = discussion.RowKey;
                        ViewBag.DiscussionTitle = discussion.MovieTitle;
                        ViewBag.Author = discussion.CreatorEmail;
                        ViewBag.Content = discussion.Synopsis;
                        ViewBag.Genre = discussion.Genre;
                        ViewBag.Year = discussion.Year;
                        ViewBag.ImdbRating = discussion.ImdbRating;
                        ViewBag.DataSource = "Azure Storage";
                    }
                }
                else
                {
                    // Fallback - simulirani podaci
                    ViewBag.DiscussionId = id;
                    ViewBag.DiscussionTitle = "Najbolji filmovi 2024 godine";
                    ViewBag.Author = "marko@example.com";
                    ViewBag.Content = "Koje filmove preporučujete za gledanje ove godine? Lično mislim da je Oppenheimer odličan film sa sjajnom glumom i režijom.";
                    ViewBag.Genre = "Drama";
                    ViewBag.Year = 2023;
                    ViewBag.ImdbRating = 8.5;
                    ViewBag.DataSource = "Test Data";
                }
                
                // Učitaj komentare iz Azure Storage
                var comments = new List<CommentViewModel>();

                try
                {
                    if (_discussionsTable != null)
                    {
                        var commentsTable = _tableClient.GetTableReference("Comments");
                        var commentsQuery = new TableQuery<CommentEntity>()
                            .Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, id));

                        var commentsResult = await commentsTable.ExecuteQuerySegmentedAsync(commentsQuery, null);

                        comments = commentsResult.Results
                            .OrderBy(c => c.CreatedUtc)
                            .Select(c => new CommentViewModel
                            {
                                Author = c.AuthorEmail,
                                Text = c.Text,
                                Date = c.CreatedUtc.ToString("dd.MM.yyyy HH:mm")
                            })
                            .ToList();
                    }

                    // Ako nema komentara iz baze, dodaj test komentare
                    if (!comments.Any())
                    {
                        comments = new List<CommentViewModel>
                        {
                            new CommentViewModel { Author = "ana@example.com", Text = "Slažem se za Oppenheimer! Odličan film.", Date = "02.09.2024" },
                            new CommentViewModel { Author = "petar@example.com", Text = "Meni se dopao i Guardians of the Galaxy Vol. 3", Date = "02.09.2024" },
                            new CommentViewModel { Author = "milica@example.com", Text = "Preporučujem Everything Everywhere All at Once!", Date = "03.09.2024" }
                        };
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Greška pri učitavanju komentara: {ex.Message}");
                    // Fallback komentari
                    comments = new List<CommentViewModel>
                    {
                        new CommentViewModel { Author = "test@example.com", Text = "Test komentar (greška pri učitavanju)", Date = DateTime.Now.ToString("dd.MM.yyyy HH:mm") }
                    };
                }

                ViewBag.Comments = comments;
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Greška pri učitavanju diskusije: {ex.Message}";
            }
            
            return View();
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> AddComment(string commentText, string discussionId)
        {
            if (!string.IsNullOrEmpty(commentText) && !string.IsNullOrEmpty(discussionId))
            {
                try
                {
                    if (_discussionsTable != null)
                    {
                        // Kreiraj komentar
                        var commentId = Guid.NewGuid().ToString();
                        var comment = new CommentEntity(discussionId, commentId)
                        {
                            AuthorEmail = User.Identity.Name,
                            Text = commentText,
                            CreatedUtc = DateTime.UtcNow
                        };

                        // Sačuvaj komentar
                        var commentsTable = _tableClient.GetTableReference("Comments");
                        commentsTable.CreateIfNotExists();
                        var insertOperation = TableOperation.Insert(comment);
                        await commentsTable.ExecuteAsync(insertOperation);

                        // Pošalji poruku u queue za notifikacije
                        var queueMessage = new QueueMessagePayload
                        {
                            DiscussionId = discussionId,
                            CommentId = commentId
                        };
                        var message = new Microsoft.WindowsAzure.Storage.Queue.CloudQueueMessage(Newtonsoft.Json.JsonConvert.SerializeObject(queueMessage));
                        await _notificationsQueue.AddMessageAsync(message);

                        TempData["SuccessMessage"] = "Komentar je uspešno dodat!";
                    }
                    else
                    {
                        TempData["SuccessMessage"] = "Komentar je uspešno dodat! (Test mode)";
                    }
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = $"Greška pri dodavanju komentara: {ex.Message}";
                }
            }
            else
            {
                TempData["ErrorMessage"] = "Molimo unesite tekst komentara.";
            }
            return RedirectToAction("Details", new { id = discussionId ?? "1" });
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Vote(string discussionId, string voteType)
        {
            if (!string.IsNullOrEmpty(discussionId) && (voteType == "positive" || voteType == "negative"))
            {
                try
                {
                    if (_discussionsTable != null)
                    {
                        // Kreiraj ili ažuriraj glas
                        var vote = new VoteEntity(discussionId, User.Identity.Name)
                        {
                            VoteType = voteType
                        };

                        var votesTable = _tableClient.GetTableReference("Votes");
                        votesTable.CreateIfNotExists();
                        var insertOperation = TableOperation.InsertOrReplace(vote);
                        await votesTable.ExecuteAsync(insertOperation);

                        // Ažuriraj broj glasova u diskusiji
                        var retrieveOperation = TableOperation.Retrieve<DiscussionEntity>("Disc", discussionId);
                        var result = await _discussionsTable.ExecuteAsync(retrieveOperation);
                        
                        if (result.Result != null)
                        {
                            var discussion = result.Result as DiscussionEntity;
                            
                            // Ponovo izračunaj glasove
                            var voteQuery = new TableQuery<VoteEntity>()
                                .Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, discussionId));
                            
                            var voteResult = await votesTable.ExecuteQuerySegmentedAsync(voteQuery, null);
                            var votes = voteResult.Results.ToList();
                            
                            discussion.Positive = votes.Count(v => v.VoteType == "positive");
                            discussion.Negative = votes.Count(v => v.VoteType == "negative");
                            
                            var updateOperation = TableOperation.InsertOrReplace(discussion);
                            await _discussionsTable.ExecuteAsync(updateOperation);
                        }

                        TempData["SuccessMessage"] = $"Uspešno ste glasali {voteType}!";
                    }
                    else
                    {
                        TempData["SuccessMessage"] = $"Uspešno ste glasali {voteType}! (Test mode)";
                    }
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = $"Greška pri glasanju: {ex.Message}";
                }
            }
            else
            {
                TempData["ErrorMessage"] = "Neispravan tip glasa.";
            }
            return RedirectToAction("Details", new { id = discussionId });
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Follow(string discussionId)
        {
            if (!string.IsNullOrEmpty(discussionId))
            {
                try
                {
                    if (_discussionsTable != null)
                    {
                        // Kreiraj ili ažuriraj praćenje
                        var follow = new FollowEntity(discussionId, User.Identity.Name);

                        var followsTable = _tableClient.GetTableReference("FollowTable");
                        followsTable.CreateIfNotExists();
                        var insertOperation = TableOperation.InsertOrReplace(follow);
                        await followsTable.ExecuteAsync(insertOperation);

                        TempData["SuccessMessage"] = "Uspešno ste počeli da pratite ovu diskusiju!";
                    }
                    else
                    {
                        TempData["SuccessMessage"] = "Uspešno ste počeli da pratite ovu diskusiju! (Test mode)";
                    }
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = $"Greška pri praćenju diskusije: {ex.Message}";
                }
            }
            else
            {
                TempData["ErrorMessage"] = "Neispravan ID diskusije.";
            }
            return RedirectToAction("Details", new { id = discussionId });
        }

        [Authorize]
        [HttpGet]
        public async Task<ActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                TempData["ErrorMessage"] = "Neispravan ID diskusije.";
                return RedirectToAction("Index");
            }

            try
            {
                if (_discussionsTable != null)
                {
                    var retrieveOperation = TableOperation.Retrieve<DiscussionEntity>("Disc", id);
                    var result = await _discussionsTable.ExecuteAsync(retrieveOperation);
                    
                    if (result.Result != null)
                    {
                        var discussion = result.Result as DiscussionEntity;
                        
                        // Proveri da li je korisnik vlasnik diskusije
                        if (discussion.CreatorEmail != User.Identity.Name)
                        {
                            TempData["ErrorMessage"] = "Možete menjati samo svoje diskusije.";
                            return RedirectToAction("Index");
                        }

                        var model = new CreateDiscussionViewModel
                        {
                            Title = discussion.MovieTitle,
                            Content = discussion.Synopsis,
                            Genre = discussion.Genre,
                            Year = discussion.Year,
                            ImdbRating = (decimal)discussion.ImdbRating,
                            Duration = discussion.DurationMin,
                            ImageUrl = discussion.PosterUrl
                        };

                        ViewBag.DiscussionId = id;
                        return View(model);
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Greška pri učitavanju diskusije: {ex.Message}";
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(string id, CreateDiscussionViewModel model)
        {
            if (string.IsNullOrEmpty(id))
            {
                TempData["ErrorMessage"] = "Neispravan ID diskusije.";
                return RedirectToAction("Index");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    if (_discussionsTable != null)
                    {
                        var retrieveOperation = TableOperation.Retrieve<DiscussionEntity>("Disc", id);
                        var result = await _discussionsTable.ExecuteAsync(retrieveOperation);
                        
                        if (result.Result != null)
                        {
                            var discussion = result.Result as DiscussionEntity;
                            
                            // Proveri da li je korisnik vlasnik diskusije
                            if (discussion.CreatorEmail != User.Identity.Name)
                            {
                                TempData["ErrorMessage"] = "Možete menjati samo svoje diskusije.";
                                return RedirectToAction("Index");
                            }

                            // Ažuriraj diskusiju
                            discussion.MovieTitle = model.Title;
                            discussion.Synopsis = model.Content;
                            discussion.Genre = model.Genre ?? "Ostalo";
                            discussion.Year = model.Year ?? 2024;
                            discussion.ImdbRating = (double)(model.ImdbRating ?? 0);
                            discussion.DurationMin = model.Duration ?? 0;
                            discussion.PosterUrl = model.ImageUrl ?? "";

                            var updateOperation = TableOperation.Replace(discussion);
                            var updateResult = await _discussionsTable.ExecuteAsync(updateOperation);

                            if (updateResult.HttpStatusCode >= 200 && updateResult.HttpStatusCode < 300)
                            {
                                TempData["SuccessMessage"] = "Diskusija je uspešno ažurirana!";
                                return RedirectToAction("Index");
                            }
                            else
                            {
                                TempData["ErrorMessage"] = "Greška pri ažuriranju diskusije.";
                            }
                        }
                        else
                        {
                            TempData["ErrorMessage"] = "Diskusija nije pronađena.";
                        }
                    }
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = $"Greška pri ažuriranju diskusije: {ex.Message}";
                }
            }

            ViewBag.DiscussionId = id;
            return View(model);
        }

        [HttpGet]
        [Authorize]
        public async Task<ActionResult> Delete(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                TempData["ErrorMessage"] = "Neispravan ID diskusije.";
                return RedirectToAction("Index");
            }

            try
            {
                if (_discussionsTable != null)
                {
                    var retrieveOperation = TableOperation.Retrieve<DiscussionEntity>("Disc", id);
                    var result = await _discussionsTable.ExecuteAsync(retrieveOperation);
                    
                    if (result.Result != null)
                    {
                        var discussion = result.Result as DiscussionEntity;
                        
                        // Proveri da li je korisnik vlasnik diskusije
                        if (discussion.CreatorEmail != User.Identity.Name)
                        {
                            TempData["ErrorMessage"] = "Možete brisati samo svoje diskusije.";
                            return RedirectToAction("Index");
                        }

                        return View(discussion);
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "Diskusija nije pronađena.";
                        return RedirectToAction("Index");
                    }
                }
                else
                {
                    TempData["ErrorMessage"] = "Greška pri pristupu bazi podataka.";
                    return RedirectToAction("Index");
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Greška pri učitavanju diskusije: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                TempData["ErrorMessage"] = "Neispravan ID diskusije.";
                return RedirectToAction("Index");
            }

            try
            {
                if (_discussionsTable != null)
                {
                    var retrieveOperation = TableOperation.Retrieve<DiscussionEntity>("Disc", id);
                    var result = await _discussionsTable.ExecuteAsync(retrieveOperation);
                    
                    if (result.Result != null)
                    {
                        var discussion = result.Result as DiscussionEntity;
                        
                        // Proveri da li je korisnik vlasnik diskusije
                        if (discussion.CreatorEmail != User.Identity.Name)
                        {
                            TempData["ErrorMessage"] = "Možete brisati samo svoje diskusije.";
                            return RedirectToAction("Index");
                        }

                        // Obriši diskusiju
                        var deleteOperation = TableOperation.Delete(discussion);
                        var deleteResult = await _discussionsTable.ExecuteAsync(deleteOperation);

                        if (deleteResult.HttpStatusCode >= 200 && deleteResult.HttpStatusCode < 300)
                        {
                            TempData["SuccessMessage"] = "Diskusija je uspešno obrisana!";
                        }
                        else
                        {
                            TempData["ErrorMessage"] = "Greška pri brisanju diskusije.";
                        }
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "Diskusija nije pronađena.";
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Greška pri brisanju diskusije: {ex.Message}";
            }

            return RedirectToAction("Index");
        }

        private async Task<bool> IsUserVerifiedAuthor(string userEmail)
        {
            try
            {
                if (_discussionsTable != null)
                {
                    var usersTable = _tableClient.GetTableReference("Users");
                    var retrieveOperation = TableOperation.Retrieve<UserEntity>("User", userEmail.ToLowerInvariant());
                    var result = await usersTable.ExecuteAsync(retrieveOperation);
                    
                    if (result.Result != null)
                    {
                        var user = result.Result as UserEntity;
                        return user.IsAuthorVerified;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error checking author verification: {ex.Message}");
            }
            
            return false; // Default to false if can't verify
        }
    }

    public class CreateDiscussionViewModel
    {
        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Naslov je obavezan")]
        [System.ComponentModel.DataAnnotations.Display(Name = "Naslov filma")]
        public string Title { get; set; }

        [System.ComponentModel.DataAnnotations.Display(Name = "Godina izlaska")]
        public int? Year { get; set; }

        [System.ComponentModel.DataAnnotations.Display(Name = "Žanr")]
        public string Genre { get; set; }

        [System.ComponentModel.DataAnnotations.Display(Name = "IMDB ocena")]
        public decimal? ImdbRating { get; set; }

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Sadržaj je obavezan")]
        [System.ComponentModel.DataAnnotations.Display(Name = "Sinopsis/Diskusija")]
        public string Content { get; set; }

        [System.ComponentModel.DataAnnotations.Display(Name = "Trajanje (minuti)")]
        public int? Duration { get; set; }

        [System.ComponentModel.DataAnnotations.Display(Name = "URL naslovne slike")]
        public string ImageUrl { get; set; }
    }

    public class DiscussionViewModel
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Author { get; set; }
        public string Date { get; set; }
        public string Genre { get; set; }
        public int Year { get; set; }
        public double ImdbRating { get; set; }
        public int Positive { get; set; }
        public int Negative { get; set; }
        public string Synopsis { get; set; }
        public string CreatorEmail { get; set; }
    }
}