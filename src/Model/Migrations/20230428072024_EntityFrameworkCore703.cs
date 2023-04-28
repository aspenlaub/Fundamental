using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aspenlaub.Net.GitHub.CSharp.Fundamental.Model.Migrations {
    /// <inheritdoc />
    public partial class EntityFrameworkCore703 : Migration {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) {
            migrationBuilder.CreateTable(
                name: "DateSummaries",
                columns: table => new {
                    Guid = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CostValueInEuro = table.Column<double>(type: "float", nullable: false),
                    QuoteValueInEuro = table.Column<double>(type: "float", nullable: false),
                    RealizedLossInEuro = table.Column<double>(type: "float", nullable: false),
                    RealizedProfitInEuro = table.Column<double>(type: "float", nullable: false),
                    UnrealizedLossInEuro = table.Column<double>(type: "float", nullable: false),
                    UnrealizedProfitInEuro = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table => {
                    table.PrimaryKey("PK_DateSummaries", x => x.Guid);
                });

            migrationBuilder.CreateTable(
                name: "Securities",
                columns: table => new {
                    Guid = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    SecurityId = table.Column<string>(type: "nvarchar(12)", maxLength: 12, nullable: true),
                    SecurityName = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    QuotedPer = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table => {
                    table.PrimaryKey("PK_Securities", x => x.Guid);
                });

            migrationBuilder.CreateTable(
                name: "Holdings",
                columns: table => new {
                    Guid = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SecurityGuid = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    NominalBalance = table.Column<double>(type: "float", nullable: false),
                    CostValueInEuro = table.Column<double>(type: "float", nullable: false),
                    QuoteValueInEuro = table.Column<double>(type: "float", nullable: false),
                    RealizedLossInEuro = table.Column<double>(type: "float", nullable: false),
                    RealizedProfitInEuro = table.Column<double>(type: "float", nullable: false),
                    UnrealizedLossInEuro = table.Column<double>(type: "float", nullable: false),
                    UnrealizedProfitInEuro = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table => {
                    table.PrimaryKey("PK_Holdings", x => x.Guid);
                    table.ForeignKey(
                        name: "FK_Holdings_Securities_SecurityGuid",
                        column: x => x.SecurityGuid,
                        principalTable: "Securities",
                        principalColumn: "Guid");
                });

            migrationBuilder.CreateTable(
                name: "Quotes",
                columns: table => new {
                    Guid = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SecurityGuid = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    PriceInEuro = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table => {
                    table.PrimaryKey("PK_Quotes", x => x.Guid);
                    table.ForeignKey(
                        name: "FK_Quotes_Securities_SecurityGuid",
                        column: x => x.SecurityGuid,
                        principalTable: "Securities",
                        principalColumn: "Guid");
                });

            migrationBuilder.CreateTable(
                name: "Transactions",
                columns: table => new {
                    Guid = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SecurityGuid = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    TransactionType = table.Column<int>(type: "int", nullable: false),
                    Nominal = table.Column<double>(type: "float", nullable: false),
                    PriceInEuro = table.Column<double>(type: "float", nullable: false),
                    ExpensesInEuro = table.Column<double>(type: "float", nullable: false),
                    IncomeInEuro = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table => {
                    table.PrimaryKey("PK_Transactions", x => x.Guid);
                    table.ForeignKey(
                        name: "FK_Transactions_Securities_SecurityGuid",
                        column: x => x.SecurityGuid,
                        principalTable: "Securities",
                        principalColumn: "Guid");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Holdings_SecurityGuid",
                table: "Holdings",
                column: "SecurityGuid");

            migrationBuilder.CreateIndex(
                name: "IX_Quotes_SecurityGuid",
                table: "Quotes",
                column: "SecurityGuid");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_SecurityGuid",
                table: "Transactions",
                column: "SecurityGuid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) {
            migrationBuilder.DropTable(
                name: "DateSummaries");

            migrationBuilder.DropTable(
                name: "Holdings");

            migrationBuilder.DropTable(
                name: "Quotes");

            migrationBuilder.DropTable(
                name: "Transactions");

            migrationBuilder.DropTable(
                name: "Securities");
        }
    }
}
