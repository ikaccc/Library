
// ai-touched

namespace Library.Lending.Infrastructure.Seeding;

internal sealed record SampleBook(string Title, string Author, int PageCount);

internal sealed record SampleBorrower(string FullName, string Email);

internal static class SampleCatalog
{
    public static IReadOnlyList<SampleBook> Books { get; } =
    [
        new("Pride and Prejudice", "Jane Austen", 432),
        new("Moby-Dick", "Herman Melville", 635),
        new("The Great Gatsby", "F. Scott Fitzgerald", 180),
        new("Jane Eyre", "Charlotte Brontë", 532),
        new("Crime and Punishment", "Fyodor Dostoevsky", 671),
        new("Frankenstein", "Mary Shelley", 280),
        new("Dracula", "Bram Stoker", 418),
        new("The Picture of Dorian Gray", "Oscar Wilde", 254),
        new("War and Peace", "Leo Tolstoy", 1225),
        new("Wuthering Heights", "Emily Brontë", 416),
        new("Great Expectations", "Charles Dickens", 544),
        new("A Tale of Two Cities", "Charles Dickens", 489),
        new("The Brothers Karamazov", "Fyodor Dostoevsky", 796),
        new("Anna Karenina", "Leo Tolstoy", 864),
        new("Les Misérables", "Victor Hugo", 1463),
        new("The Count of Monte Cristo", "Alexandre Dumas", 1276),
        new("Don Quixote", "Miguel de Cervantes", 1072),
        new("The Odyssey", "Homer", 541),
        new("The Iliad", "Homer", 704),
        new("Madame Bovary", "Gustave Flaubert", 329),
        new("Middlemarch", "George Eliot", 904),
        new("The Adventures of Huckleberry Finn", "Mark Twain", 366),
        new("The Adventures of Tom Sawyer", "Mark Twain", 274),
        new("Little Women", "Louisa May Alcott", 449),
        new("Sense and Sensibility", "Jane Austen", 409),
        new("Emma", "Jane Austen", 474),
        new("Persuasion", "Jane Austen", 249),
        new("The Scarlet Letter", "Nathaniel Hawthorne", 238),
        new("Heart of Darkness", "Joseph Conrad", 111),
        new("The Metamorphosis", "Franz Kafka", 201),
        new("The Trial", "Franz Kafka", 255),
        new("Ulysses", "James Joyce", 730),
        new("Dubliners", "James Joyce", 152),
        new("Treasure Island", "Robert Louis Stevenson", 311),
        new("Strange Case of Dr Jekyll and Mr Hyde", "Robert Louis Stevenson", 141),
        new("The Call of the Wild", "Jack London", 232),
        new("Twenty Thousand Leagues Under the Seas", "Jules Verne", 419),
        new("Around the World in Eighty Days", "Jules Verne", 252),
        new("The Time Machine", "H. G. Wells", 118),
        new("The War of the Worlds", "H. G. Wells", 192),
    ];

    public static IReadOnlyList<SampleBorrower> Borrowers { get; } =
    [
        new("Ana Petrova", "ana.petrova@example.com"),
        new("Marko Nikolov", "marko.nikolov@example.com"),
        new("Elena Stojanova", "elena.stojanova@example.com"),
        new("Nikola Trajkov", "nikola.trajkov@example.com"),
        new("Sara Ahmed", "sara.ahmed@example.com"),
        new("Liam O'Connor", "liam.oconnor@example.com"),
        new("Yuki Tanaka", "yuki.tanaka@example.com"),
        new("Amara Okafor", "amara.okafor@example.com"),
        new("Mateo Rossi", "mateo.rossi@example.com"),
        new("Ingrid Larsen", "ingrid.larsen@example.com"),
        new("Tomás Herrera", "tomas.herrera@example.com"),
        new("Priya Nair", "priya.nair@example.com"),
        new("Jonas Weber", "jonas.weber@example.com"),
        new("Fatima Zahra", "fatima.zahra@example.com"),
        new("Oliver Bennett", "oliver.bennett@example.com"),
        new("Mei Chen", "mei.chen@example.com"),
        new("Lukas Novák", "lukas.novak@example.com"),
        new("Zainab Hussain", "zainab.hussain@example.com"),
        new("Daniel Kim", "daniel.kim@example.com"),
        new("Sofia Andersson", "sofia.andersson@example.com"),
        new("Emil Johansen", "emil.johansen@example.com"),
        new("Chloé Dubois", "chloe.dubois@example.com"),
        new("Ivan Georgiev", "ivan.georgiev@example.com"),
        new("Hana Kováčová", "hana.kovacova@example.com"),
        new("Noah Williams", "noah.williams@example.com"),
    ];
}
