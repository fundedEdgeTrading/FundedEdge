using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace FundedEdge.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AiReports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    Question = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Content = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Model = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    InputTokens = table.Column<int>(type: "integer", nullable: false),
                    OutputTokens = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiReports", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    DisplayName = table.Column<string>(type: "text", nullable: true),
                    AvatarUrl = table.Column<string>(type: "text", nullable: true),
                    PlanTier = table.Column<int>(type: "integer", nullable: false),
                    TrialEndsAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    StripeCustomerId = table.Column<string>(type: "text", nullable: true),
                    UserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: true),
                    SecurityStamp = table.Column<string>(type: "text", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true),
                    PhoneNumber = table.Column<string>(type: "text", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DailyMindsetCheckIns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    SleepQuality = table.Column<int>(type: "integer", nullable: false),
                    ExternalStress = table.Column<int>(type: "integer", nullable: false),
                    PreMarketFocus = table.Column<int>(type: "integer", nullable: false),
                    DominantPreMarketEmotion = table.Column<int>(type: "integer", nullable: false),
                    Note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyMindsetCheckIns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Instruments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Root = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TickSize = table.Column<decimal>(type: "numeric(18,8)", nullable: false),
                    TickValue = table.Column<decimal>(type: "numeric(18,4)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Instruments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IntegrationSettings",
                columns: table => new
                {
                    Key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ProtectedValue = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntegrationSettings", x => x.Key);
                });

            migrationBuilder.CreateTable(
                name: "ProcessedWebhookEvents",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ProcessedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessedWebhookEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PropFirms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Website = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    MinDaysBetweenPayouts = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PropFirms", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PublicProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    Slug = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PublicProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TradeSetups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TradeSetups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoleId = table.Column<string>(type: "text", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    ProviderKey = table.Column<string>(type: "text", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "text", nullable: true),
                    UserId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    RoleId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EvaluationPrograms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PropFirmId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    AccountSize = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    EvaluationCost = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ActivationCost = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ProfitTarget = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    MaxDrawdown = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    DrawdownType = table.Column<int>(type: "integer", nullable: false),
                    DailyLossLimit = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    MinTradingDays = table.Column<int>(type: "integer", nullable: true),
                    ConsistencyMaxDayFraction = table.Column<decimal>(type: "numeric(5,4)", nullable: true),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    FundedMaxDrawdown = table.Column<decimal>(type: "numeric", nullable: true),
                    FundedDrawdownType = table.Column<int>(type: "integer", nullable: true),
                    FundedDailyLossLimit = table.Column<decimal>(type: "numeric", nullable: true),
                    FundedProfitTarget = table.Column<decimal>(type: "numeric", nullable: true),
                    FundedMinTradingDays = table.Column<int>(type: "integer", nullable: true),
                    PayoutSplitTraderPct = table.Column<decimal>(type: "numeric", nullable: false),
                    PayoutMaxProfitPct = table.Column<decimal>(type: "numeric", nullable: true),
                    PayoutMinDaysBetween = table.Column<int>(type: "integer", nullable: true),
                    PropFirmId1 = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvaluationPrograms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EvaluationPrograms_PropFirms_PropFirmId",
                        column: x => x.PropFirmId,
                        principalTable: "PropFirms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EvaluationPrograms_PropFirms_PropFirmId1",
                        column: x => x.PropFirmId1,
                        principalTable: "PropFirms",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TradingAccounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    PropFirmId = table.Column<Guid>(type: "uuid", nullable: false),
                    EvaluationProgramId = table.Column<Guid>(type: "uuid", nullable: true),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ExternalAccountId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    AccountSize = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ProfitTarget = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    MaxDrawdown = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    DrawdownType = table.Column<int>(type: "integer", nullable: false),
                    Stage = table.Column<int>(type: "integer", nullable: false),
                    Feed = table.Column<int>(type: "integer", nullable: false),
                    PurchasedOn = table.Column<DateOnly>(type: "date", nullable: false),
                    FundedOn = table.Column<DateOnly>(type: "date", nullable: true),
                    ClosedOn = table.Column<DateOnly>(type: "date", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TradingAccounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TradingAccounts_EvaluationPrograms_EvaluationProgramId",
                        column: x => x.EvaluationProgramId,
                        principalTable: "EvaluationPrograms",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TradingAccounts_PropFirms_PropFirmId",
                        column: x => x.PropFirmId,
                        principalTable: "PropFirms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AccountCosts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    PaidOn = table.Column<DateOnly>(type: "date", nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountCosts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccountCosts_TradingAccounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "TradingAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AccountEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    FromStage = table.Column<int>(type: "integer", nullable: false),
                    ToStage = table.Column<int>(type: "integer", nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccountEvents_TradingAccounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "TradingAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Payouts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    AmountRequested = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    AmountReceived = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    RequestedOn = table.Column<DateOnly>(type: "date", nullable: false),
                    PaidOn = table.Column<DateOnly>(type: "date", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payouts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Payouts_TradingAccounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "TradingAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Trades",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    InstrumentId = table.Column<Guid>(type: "uuid", nullable: true),
                    Symbol = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Direction = table.Column<int>(type: "integer", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    AvgEntryPrice = table.Column<decimal>(type: "numeric(18,8)", nullable: false),
                    AvgExitPrice = table.Column<decimal>(type: "numeric(18,8)", nullable: false),
                    OpenedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ClosedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    GrossPnL = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Commissions = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    RiskedAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    MaxAdverseExcursion = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    MaxFavorableExcursion = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    Tags = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Trades", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Trades_Instruments_InstrumentId",
                        column: x => x.InstrumentId,
                        principalTable: "Instruments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Trades_TradingAccounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "TradingAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Executions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExternalId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Source = table.Column<int>(type: "integer", nullable: false),
                    Symbol = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Side = table.Column<int>(type: "integer", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    Price = table.Column<decimal>(type: "numeric(18,8)", nullable: false),
                    ExecutedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Commission = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    TradeId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Executions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Executions_Trades_TradeId",
                        column: x => x.TradeId,
                        principalTable: "Trades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Executions_TradingAccounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "TradingAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TradeEmotionLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TradeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Moment = table.Column<int>(type: "integer", nullable: false),
                    Emotion = table.Column<int>(type: "integer", nullable: false),
                    Intensity = table.Column<int>(type: "integer", nullable: false),
                    Adherence = table.Column<int>(type: "integer", nullable: false),
                    WasImpulsive = table.Column<bool>(type: "boolean", nullable: false),
                    Note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    LoggedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TradeEmotionLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TradeEmotionLogs_Trades_TradeId",
                        column: x => x.TradeId,
                        principalTable: "Trades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Instruments",
                columns: new[] { "Id", "Name", "Root", "TickSize", "TickValue" },
                values: new object[,]
                {
                    { new Guid("aaaaaaaa-0000-0000-0000-000000000001"), "E-mini S&P 500", "ES", 0.25m, 12.50m },
                    { new Guid("aaaaaaaa-0000-0000-0000-000000000002"), "Micro E-mini S&P 500", "MES", 0.25m, 1.25m },
                    { new Guid("aaaaaaaa-0000-0000-0000-000000000003"), "E-mini Nasdaq-100", "NQ", 0.25m, 5.00m },
                    { new Guid("aaaaaaaa-0000-0000-0000-000000000004"), "Micro E-mini Nasdaq-100", "MNQ", 0.25m, 0.50m },
                    { new Guid("aaaaaaaa-0000-0000-0000-000000000005"), "Gold Futures", "GC", 0.10m, 10.00m },
                    { new Guid("aaaaaaaa-0000-0000-0000-000000000006"), "Crude Oil Futures", "CL", 0.01m, 10.00m }
                });

            migrationBuilder.InsertData(
                table: "PropFirms",
                columns: new[] { "Id", "MinDaysBetweenPayouts", "Name", "Notes", "Website" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111111"), null, "Lucid Trading", null, "https://lucidtrading.com" },
                    { new Guid("22222222-2222-2222-2222-222222222222"), null, "Tradeify", null, "https://tradeify.co" },
                    { new Guid("33333333-3333-3333-3333-333333333333"), null, "Apex Trader Funding", null, "https://apextraderfunding.com" },
                    { new Guid("44444444-4444-4444-4444-444444444444"), null, "Topstep", null, "https://topstep.com" },
                    { new Guid("55555555-5555-5555-5555-555555555555"), null, "MyFundedFutures", null, "https://myfundedfutures.com" },
                    { new Guid("66666666-6666-6666-6666-666666666666"), null, "Take Profit Trader", null, "https://takeprofittrader.com" },
                    { new Guid("77777777-7777-7777-7777-777777777777"), null, "Earn2Trade", null, "https://earn2trade.com" }
                });

            migrationBuilder.InsertData(
                table: "EvaluationPrograms",
                columns: new[] { "Id", "AccountSize", "ActivationCost", "ConsistencyMaxDayFraction", "DailyLossLimit", "DrawdownType", "EffectiveFrom", "EvaluationCost", "FundedDailyLossLimit", "FundedDrawdownType", "FundedMaxDrawdown", "FundedMinTradingDays", "FundedProfitTarget", "IsActive", "MaxDrawdown", "MinTradingDays", "Name", "PayoutMaxProfitPct", "PayoutMinDaysBetween", "PayoutSplitTraderPct", "ProfitTarget", "PropFirmId", "PropFirmId1" },
                values: new object[,]
                {
                    { new Guid("b0000000-0000-0000-0000-000000000001"), 50000m, 130m, 0.30m, null, 0, new DateOnly(2026, 1, 1), 167m, null, 0, 2500m, 7, null, true, 2500m, 7, "Apex 50K", null, 7, 1.00m, 3000m, new Guid("33333333-3333-3333-3333-333333333333"), null },
                    { new Guid("b0000000-0000-0000-0000-000000000002"), 100000m, 130m, 0.30m, null, 0, new DateOnly(2026, 1, 1), 207m, null, 0, 3000m, 7, null, true, 3000m, 7, "Apex 100K", null, 7, 1.00m, 6000m, new Guid("33333333-3333-3333-3333-333333333333"), null },
                    { new Guid("b0000000-0000-0000-0000-000000000003"), 50000m, 0m, null, 1250m, 1, new DateOnly(2026, 1, 1), 165m, 1250m, 1, 2000m, null, null, true, 2000m, 5, "Tradeify Growth 50K", null, 14, 0.90m, 3000m, new Guid("22222222-2222-2222-2222-222222222222"), null },
                    { new Guid("b0000000-0000-0000-0000-000000000004"), 100000m, 0m, null, 2500m, 1, new DateOnly(2026, 1, 1), 219m, 2500m, 1, 3000m, null, null, true, 3000m, 5, "Tradeify Advanced 100K", null, 14, 0.90m, 6000m, new Guid("22222222-2222-2222-2222-222222222222"), null },
                    { new Guid("b0000000-0000-0000-0000-000000000005"), 50000m, 0m, null, 1250m, 2, new DateOnly(2026, 1, 1), 137m, 1250m, 2, 2000m, null, null, true, 2000m, null, "Lucid 50K", 0.50m, 14, 0.90m, 2000m, new Guid("11111111-1111-1111-1111-111111111111"), null },
                    { new Guid("b0000000-0000-0000-0000-000000000006"), 100000m, 0m, null, 2500m, 2, new DateOnly(2026, 1, 1), 267m, 2500m, 2, 3000m, null, null, true, 3000m, null, "Lucid 100K", 0.50m, 14, 0.90m, 4000m, new Guid("11111111-1111-1111-1111-111111111111"), null },
                    { new Guid("b0000000-0000-0000-0000-000000000007"), 50000m, 149m, null, 1000m, 0, new DateOnly(2026, 1, 1), 165m, 1000m, 0, 2000m, null, null, true, 2000m, 5, "Topstep 50K", null, 14, 0.90m, 3000m, new Guid("44444444-4444-4444-4444-444444444444"), null },
                    { new Guid("b0000000-0000-0000-0000-000000000008"), 100000m, 149m, null, 2000m, 0, new DateOnly(2026, 1, 1), 245m, 2000m, 0, 3000m, null, null, true, 3000m, 5, "Topstep 100K", null, 14, 0.90m, 6000m, new Guid("44444444-4444-4444-4444-444444444444"), null },
                    { new Guid("b0000000-0000-0000-0000-000000000009"), 50000m, 0m, 0.50m, null, 0, new DateOnly(2026, 1, 1), 165m, null, 0, 2000m, null, null, true, 2000m, null, "MFF Rapid 50K", null, 5, 0.90m, 3000m, new Guid("55555555-5555-5555-5555-555555555555"), null },
                    { new Guid("b0000000-0000-0000-0000-000000000010"), 100000m, 0m, 0.50m, null, 0, new DateOnly(2026, 1, 1), 250m, null, 0, 3000m, null, null, true, 3000m, null, "MFF Rapid 100K", null, 5, 0.90m, 6000m, new Guid("55555555-5555-5555-5555-555555555555"), null },
                    { new Guid("b0000000-0000-0000-0000-000000000011"), 50000m, 130m, null, null, 1, new DateOnly(2026, 1, 1), 150m, null, 0, 2000m, null, null, true, 2000m, 5, "TPT 50K", null, 14, 0.80m, 3000m, new Guid("66666666-6666-6666-6666-666666666666"), null },
                    { new Guid("b0000000-0000-0000-0000-000000000012"), 100000m, 130m, null, null, 1, new DateOnly(2026, 1, 1), 220m, null, 0, 3000m, null, null, true, 3000m, 5, "TPT 100K", null, 14, 0.80m, 6000m, new Guid("66666666-6666-6666-6666-666666666666"), null },
                    { new Guid("b0000000-0000-0000-0000-000000000013"), 100000m, 0m, 0.30m, 2200m, 1, new DateOnly(2026, 1, 1), 430m, null, 1, 4000m, null, null, true, 4000m, 10, "E2T Gauntlet Mini 100K", null, 30, 0.80m, 6000m, new Guid("77777777-7777-7777-7777-777777777777"), null },
                    { new Guid("b0000000-0000-0000-0000-000000000014"), 50000m, 0m, 0.30m, 1100m, 1, new DateOnly(2026, 1, 1), 245m, null, 1, 2000m, null, null, true, 2000m, 10, "E2T Gauntlet Mini 50K", null, 30, 0.80m, 3000m, new Guid("77777777-7777-7777-7777-777777777777"), null },
                    { new Guid("b0000000-0000-0000-0000-000000000015"), 25000m, 130m, 0.30m, null, 0, new DateOnly(2026, 1, 1), 147m, null, 0, 1500m, 7, null, true, 1500m, 7, "Apex 25K", null, 7, 1.00m, 1500m, new Guid("33333333-3333-3333-3333-333333333333"), null },
                    { new Guid("b0000000-0000-0000-0000-000000000016"), 150000m, 130m, 0.30m, null, 0, new DateOnly(2026, 1, 1), 297m, null, 0, 5000m, 7, null, true, 5000m, 7, "Apex 150K", null, 7, 1.00m, 9000m, new Guid("33333333-3333-3333-3333-333333333333"), null },
                    { new Guid("b0000000-0000-0000-0000-000000000017"), 25000m, 0m, null, 600m, 1, new DateOnly(2026, 1, 1), 130m, 600m, 1, 1000m, null, null, true, 1000m, 5, "Tradeify Growth 25K", null, 14, 0.90m, 1500m, new Guid("22222222-2222-2222-2222-222222222222"), null },
                    { new Guid("b0000000-0000-0000-0000-000000000018"), 150000m, 0m, null, 3750m, 1, new DateOnly(2026, 1, 1), 275m, 3750m, 1, 5000m, null, null, true, 5000m, 5, "Tradeify Growth 150K", null, 14, 0.90m, 9000m, new Guid("22222222-2222-2222-2222-222222222222"), null },
                    { new Guid("b0000000-0000-0000-0000-000000000019"), 25000m, 0m, null, 625m, 2, new DateOnly(2026, 1, 1), 95m, 625m, 2, 1000m, null, null, true, 1000m, null, "Lucid 25K", 0.50m, 14, 0.90m, 1000m, new Guid("11111111-1111-1111-1111-111111111111"), null },
                    { new Guid("b0000000-0000-0000-0000-000000000020"), 150000m, 0m, null, 3750m, 2, new DateOnly(2026, 1, 1), 365m, 3750m, 2, 4500m, null, null, true, 4500m, null, "Lucid 150K", 0.50m, 14, 0.90m, 6000m, new Guid("11111111-1111-1111-1111-111111111111"), null },
                    { new Guid("b0000000-0000-0000-0000-000000000021"), 150000m, 149m, null, 3000m, 0, new DateOnly(2026, 1, 1), 199m, 3000m, 0, 4500m, null, null, true, 4500m, 5, "Topstep 150K", null, 14, 0.90m, 9000m, new Guid("44444444-4444-4444-4444-444444444444"), null },
                    { new Guid("b0000000-0000-0000-0000-000000000022"), 25000m, 0m, 0.50m, null, 0, new DateOnly(2026, 1, 1), 120m, null, 0, 1000m, null, null, true, 1000m, null, "MFF Rapid 25K", null, 5, 0.90m, 1500m, new Guid("55555555-5555-5555-5555-555555555555"), null },
                    { new Guid("b0000000-0000-0000-0000-000000000023"), 150000m, 0m, 0.50m, null, 0, new DateOnly(2026, 1, 1), 320m, null, 0, 4500m, null, null, true, 4500m, null, "MFF Rapid 150K", null, 5, 0.90m, 9000m, new Guid("55555555-5555-5555-5555-555555555555"), null },
                    { new Guid("b0000000-0000-0000-0000-000000000024"), 25000m, 130m, null, null, 1, new DateOnly(2026, 1, 1), 150m, null, 0, 1500m, null, null, true, 1500m, 5, "TPT 25K", null, 14, 0.80m, 1500m, new Guid("66666666-6666-6666-6666-666666666666"), null },
                    { new Guid("b0000000-0000-0000-0000-000000000025"), 75000m, 130m, null, null, 1, new DateOnly(2026, 1, 1), 185m, null, 0, 3000m, null, null, true, 3000m, 5, "TPT 75K", null, 14, 0.80m, 4500m, new Guid("66666666-6666-6666-6666-666666666666"), null },
                    { new Guid("b0000000-0000-0000-0000-000000000026"), 150000m, 130m, null, null, 1, new DateOnly(2026, 1, 1), 300m, null, 0, 4500m, null, null, true, 4500m, 5, "TPT 150K", null, 14, 0.80m, 9000m, new Guid("66666666-6666-6666-6666-666666666666"), null },
                    { new Guid("b0000000-0000-0000-0000-000000000027"), 150000m, 0m, 0.30m, 3300m, 1, new DateOnly(2026, 1, 1), 600m, null, 1, 6000m, null, null, true, 6000m, 10, "E2T Gauntlet Mini 150K", null, 30, 0.80m, 9000m, new Guid("77777777-7777-7777-7777-777777777777"), null },
                    { new Guid("b0000000-0000-0000-0000-000000000028"), 200000m, 0m, 0.30m, 4400m, 1, new DateOnly(2026, 1, 1), 750m, null, 1, 8000m, null, null, true, 8000m, 10, "E2T Gauntlet Mini 200K", null, 30, 0.80m, 12000m, new Guid("77777777-7777-7777-7777-777777777777"), null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccountCosts_AccountId_PaidOn",
                table: "AccountCosts",
                columns: new[] { "AccountId", "PaidOn" });

            migrationBuilder.CreateIndex(
                name: "IX_AccountEvents_AccountId_OccurredAt",
                table: "AccountEvents",
                columns: new[] { "AccountId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AiReports_CreatedAt",
                table: "AiReports",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AiReports_UserId",
                table: "AiReports",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DailyMindsetCheckIns_UserId_Date",
                table: "DailyMindsetCheckIns",
                columns: new[] { "UserId", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationPrograms_PropFirmId_IsActive",
                table: "EvaluationPrograms",
                columns: new[] { "PropFirmId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationPrograms_PropFirmId1",
                table: "EvaluationPrograms",
                column: "PropFirmId1");

            migrationBuilder.CreateIndex(
                name: "IX_Executions_AccountId_ExecutedAt",
                table: "Executions",
                columns: new[] { "AccountId", "ExecutedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Executions_Source_ExternalId",
                table: "Executions",
                columns: new[] { "Source", "ExternalId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Executions_TradeId",
                table: "Executions",
                column: "TradeId");

            migrationBuilder.CreateIndex(
                name: "IX_Instruments_Root",
                table: "Instruments",
                column: "Root",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payouts_AccountId_RequestedOn",
                table: "Payouts",
                columns: new[] { "AccountId", "RequestedOn" });

            migrationBuilder.CreateIndex(
                name: "IX_PropFirms_Name",
                table: "PropFirms",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PublicProfiles_Slug",
                table: "PublicProfiles",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PublicProfiles_UserId",
                table: "PublicProfiles",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TradeEmotionLogs_TradeId",
                table: "TradeEmotionLogs",
                column: "TradeId");

            migrationBuilder.CreateIndex(
                name: "IX_Trades_AccountId_ClosedAt",
                table: "Trades",
                columns: new[] { "AccountId", "ClosedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Trades_InstrumentId",
                table: "Trades",
                column: "InstrumentId");

            migrationBuilder.CreateIndex(
                name: "IX_TradeSetups_UserId_Name",
                table: "TradeSetups",
                columns: new[] { "UserId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TradingAccounts_EvaluationProgramId",
                table: "TradingAccounts",
                column: "EvaluationProgramId");

            migrationBuilder.CreateIndex(
                name: "IX_TradingAccounts_PropFirmId",
                table: "TradingAccounts",
                column: "PropFirmId");

            migrationBuilder.CreateIndex(
                name: "IX_TradingAccounts_Stage",
                table: "TradingAccounts",
                column: "Stage");

            migrationBuilder.CreateIndex(
                name: "IX_TradingAccounts_UserId",
                table: "TradingAccounts",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccountCosts");

            migrationBuilder.DropTable(
                name: "AccountEvents");

            migrationBuilder.DropTable(
                name: "AiReports");

            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "DailyMindsetCheckIns");

            migrationBuilder.DropTable(
                name: "Executions");

            migrationBuilder.DropTable(
                name: "IntegrationSettings");

            migrationBuilder.DropTable(
                name: "Payouts");

            migrationBuilder.DropTable(
                name: "ProcessedWebhookEvents");

            migrationBuilder.DropTable(
                name: "PublicProfiles");

            migrationBuilder.DropTable(
                name: "TradeEmotionLogs");

            migrationBuilder.DropTable(
                name: "TradeSetups");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "Trades");

            migrationBuilder.DropTable(
                name: "Instruments");

            migrationBuilder.DropTable(
                name: "TradingAccounts");

            migrationBuilder.DropTable(
                name: "EvaluationPrograms");

            migrationBuilder.DropTable(
                name: "PropFirms");
        }
    }
}
