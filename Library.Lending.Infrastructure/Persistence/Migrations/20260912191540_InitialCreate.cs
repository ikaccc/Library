using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Library.Lending.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "lending");

            migrationBuilder.CreateTable(
                name: "books",
                schema: "lending",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    author = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    isbn = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: true),
                    page_count = table.Column<int>(type: "integer", nullable: false),
                    total_copies = table.Column<int>(type: "integer", nullable: false),
                    available_copies = table.Column<int>(type: "integer", nullable: false),
                    registered_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_books", x => x.id);
                    table.CheckConstraint("ck_books_available_copies_in_range", "available_copies >= 0 AND available_copies <= total_copies");
                    table.CheckConstraint("ck_books_page_count_positive", "page_count > 0");
                    table.CheckConstraint("ck_books_total_copies_positive", "total_copies > 0");
                });

            migrationBuilder.CreateTable(
                name: "borrowers",
                schema: "lending",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    joined_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_borrowers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "loans",
                schema: "lending",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    book_id = table.Column<Guid>(type: "uuid", nullable: false),
                    borrower_id = table.Column<Guid>(type: "uuid", nullable: false),
                    borrowed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    due_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    returned_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_loans", x => x.id);
                    table.CheckConstraint("ck_loans_due_after_borrowed", "due_at > borrowed_at");
                    table.CheckConstraint("ck_loans_returned_after_borrowed", "returned_at IS NULL OR returned_at >= borrowed_at");
                    table.ForeignKey(
                        name: "fk_loans_books_book_id",
                        column: x => x.book_id,
                        principalSchema: "lending",
                        principalTable: "books",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_loans_borrowers_borrower_id",
                        column: x => x.borrower_id,
                        principalSchema: "lending",
                        principalTable: "borrowers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_books_isbn",
                schema: "lending",
                table: "books",
                column: "isbn",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_books_title",
                schema: "lending",
                table: "books",
                column: "title");

            migrationBuilder.CreateIndex(
                name: "ix_borrowers_full_name",
                schema: "lending",
                table: "borrowers",
                column: "full_name");

            migrationBuilder.CreateIndex(
                name: "ix_loans_book_id",
                schema: "lending",
                table: "loans",
                column: "book_id");

            migrationBuilder.CreateIndex(
                name: "ix_loans_borrowed_at",
                schema: "lending",
                table: "loans",
                column: "borrowed_at");

            migrationBuilder.CreateIndex(
                name: "ix_loans_borrower_id_open",
                schema: "lending",
                table: "loans",
                column: "borrower_id",
                filter: "returned_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_loans_borrower_id_returned_at",
                schema: "lending",
                table: "loans",
                columns: new[] { "borrower_id", "returned_at" });

            migrationBuilder.CreateIndex(
                name: "ix_loans_returned_at",
                schema: "lending",
                table: "loans",
                column: "returned_at");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "loans",
                schema: "lending");

            migrationBuilder.DropTable(
                name: "books",
                schema: "lending");

            migrationBuilder.DropTable(
                name: "borrowers",
                schema: "lending");
        }
    }
}
