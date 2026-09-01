using Microsoft.Data.Sqlite;

namespace SakerLabb.Web.Data;

public class TicketRepository
{
    private readonly Db _db;
    private readonly ILogger<TicketRepository> _logger;

    public TicketRepository(Db db, ILogger<TicketRepository> logger)
    {
        _db = db;
        _logger = logger;
    }

    private static readonly HashSet<string> AllowedSortColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        "t.Id", "t.Id DESC", "t.Title", "t.Title DESC",
        "t.Status", "t.Status DESC", "t.Priority", "t.Priority DESC",
        "t.Created", "t.Created DESC"
    };

    public List<Ticket> Search(string? search, string? sort)
    {
        using var connection = _db.Open();
        var command = connection.CreateCommand();

        var order = AllowedSortColumns.Contains(sort ?? "") ? sort! : "t.Id DESC";

        if (string.IsNullOrWhiteSpace(search))
        {
            command.CommandText = "SELECT t.Id, t.Title, t.Body, t.Status, t.Priority, t.OwnerId, t.Internal, t.Created, u.Username "
                + "FROM Tickets t JOIN Users u ON u.Id = t.OwnerId ORDER BY " + order;
        }
        else
        {
            command.CommandText = "SELECT t.Id, t.Title, t.Body, t.Status, t.Priority, t.OwnerId, t.Internal, t.Created, u.Username "
                + "FROM Tickets t JOIN Users u ON u.Id = t.OwnerId WHERE (t.Title LIKE '%' || $search || '%' OR t.Body LIKE '%' || $search || '%') ORDER BY " + order;
            command.Parameters.AddWithValue("$search", search);
        }

        _logger.LogInformation("Ärendesökning utförd med fritext {Search} och sortering {Sort}", search, order);

        return Read(command);
    }

    public Ticket? GetById(string id)
    {
        using var connection = _db.Open();
        var command = connection.CreateCommand();
        command.CommandText = "SELECT t.Id, t.Title, t.Body, t.Status, t.Priority, t.OwnerId, t.Internal, t.Created, u.Username "
            + "FROM Tickets t JOIN Users u ON u.Id = t.OwnerId WHERE t.Id = $id";
        command.Parameters.AddWithValue("$id", id);

        return Read(command).FirstOrDefault();
    }

    public List<Comment> GetComments(string ticketId)
    {
        using var connection = _db.Open();
        var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, TicketId, Author, Text, Created FROM Comments WHERE TicketId = $ticketId";
        command.Parameters.AddWithValue("$ticketId", ticketId);

        var comments = new List<Comment>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            comments.Add(new Comment
            {
                Id = reader.GetInt32(0),
                TicketId = reader.GetInt32(1),
                Author = reader.GetString(2),
                Text = reader.GetString(3),
                Created = reader.GetString(4)
            });
        }

        return comments;
    }

    public void AddComment(string ticketId, string author, string text)
    {
        using var connection = _db.Open();
        var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO Comments (TicketId, Author, Text, Created) VALUES ($ticketId, $author, $text, $created)";
        command.Parameters.AddWithValue("$ticketId", ticketId);
        command.Parameters.AddWithValue("$author", author);
        command.Parameters.AddWithValue("$text", text);
        command.Parameters.AddWithValue("$created", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm"));
        command.ExecuteNonQuery();
    }

    public void UpdateStatus(string ticketId, string status)
    {
        using var connection = _db.Open();
        var command = connection.CreateCommand();
        command.CommandText = "UPDATE Tickets SET Status = $status WHERE Id = $ticketId";
        command.Parameters.AddWithValue("$status", status);
        command.Parameters.AddWithValue("$ticketId", ticketId);
        command.ExecuteNonQuery();
    }

    private static List<Ticket> Read(SqliteCommand command)
    {
        var tickets = new List<Ticket>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            tickets.Add(new Ticket
            {
                Id = reader.GetInt32(0),
                Title = reader.GetString(1),
                Body = reader.GetString(2),
                Status = reader.GetString(3),
                Priority = reader.GetString(4),
                OwnerId = reader.GetInt32(5),
                Internal = reader.GetInt32(6) == 1,
                Created = reader.GetString(7),
                OwnerName = reader.GetString(8)
            });
        }

        return tickets;
    }
}
