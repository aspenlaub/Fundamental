﻿// <auto-generated />
using System;
using Aspenlaub.Net.GitHub.CSharp.Fundamental.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace Aspenlaub.Net.GitHub.CSharp.Fundamental.Model.Migrations
{
    [DbContext(typeof(Context))]
    [Migration("20240602104513_MicrosoftEntityFrameworkCoreTools806")]
    partial class MicrosoftEntityFrameworkCoreTools806
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.6")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("Aspenlaub.Net.GitHub.CSharp.Fundamental.Model.Entities.DateSummary", b =>
                {
                    b.Property<string>("Guid")
                        .HasColumnType("nvarchar(450)");

                    b.Property<double>("CostValueInEuro")
                        .HasColumnType("float");

                    b.Property<DateTime>("Date")
                        .HasColumnType("datetime2");

                    b.Property<double>("QuoteValueInEuro")
                        .HasColumnType("float");

                    b.Property<double>("RealizedLossInEuro")
                        .HasColumnType("float");

                    b.Property<double>("RealizedProfitInEuro")
                        .HasColumnType("float");

                    b.Property<double>("UnrealizedLossInEuro")
                        .HasColumnType("float");

                    b.Property<double>("UnrealizedProfitInEuro")
                        .HasColumnType("float");

                    b.HasKey("Guid");

                    b.ToTable("DateSummaries");
                });

            modelBuilder.Entity("Aspenlaub.Net.GitHub.CSharp.Fundamental.Model.Entities.Holding", b =>
                {
                    b.Property<string>("Guid")
                        .HasColumnType("nvarchar(450)");

                    b.Property<double>("CostValueInEuro")
                        .HasColumnType("float");

                    b.Property<DateTime>("Date")
                        .HasColumnType("datetime2");

                    b.Property<double>("NominalBalance")
                        .HasColumnType("float");

                    b.Property<double>("QuoteValueInEuro")
                        .HasColumnType("float");

                    b.Property<double>("RealizedLossInEuro")
                        .HasColumnType("float");

                    b.Property<double>("RealizedProfitInEuro")
                        .HasColumnType("float");

                    b.Property<string>("SecurityGuid")
                        .HasColumnType("nvarchar(450)");

                    b.Property<double>("UnrealizedLossInEuro")
                        .HasColumnType("float");

                    b.Property<double>("UnrealizedProfitInEuro")
                        .HasColumnType("float");

                    b.HasKey("Guid");

                    b.HasIndex("SecurityGuid");

                    b.ToTable("Holdings");
                });

            modelBuilder.Entity("Aspenlaub.Net.GitHub.CSharp.Fundamental.Model.Entities.Quote", b =>
                {
                    b.Property<string>("Guid")
                        .HasColumnType("nvarchar(450)");

                    b.Property<DateTime>("Date")
                        .HasColumnType("datetime2");

                    b.Property<double>("PriceInEuro")
                        .HasColumnType("float");

                    b.Property<string>("SecurityGuid")
                        .HasColumnType("nvarchar(450)");

                    b.HasKey("Guid");

                    b.HasIndex("SecurityGuid");

                    b.ToTable("Quotes");
                });

            modelBuilder.Entity("Aspenlaub.Net.GitHub.CSharp.Fundamental.Model.Entities.Security", b =>
                {
                    b.Property<string>("Guid")
                        .HasColumnType("nvarchar(450)");

                    b.Property<double>("QuotedPer")
                        .HasColumnType("float");

                    b.Property<string>("SecurityId")
                        .HasMaxLength(12)
                        .HasColumnType("nvarchar(12)");

                    b.Property<string>("SecurityName")
                        .HasMaxLength(64)
                        .HasColumnType("nvarchar(64)");

                    b.HasKey("Guid");

                    b.ToTable("Securities");
                });

            modelBuilder.Entity("Aspenlaub.Net.GitHub.CSharp.Fundamental.Model.Entities.Transaction", b =>
                {
                    b.Property<string>("Guid")
                        .HasColumnType("nvarchar(450)");

                    b.Property<DateTime>("Date")
                        .HasColumnType("datetime2");

                    b.Property<double>("ExpensesInEuro")
                        .HasColumnType("float");

                    b.Property<double>("IncomeInEuro")
                        .HasColumnType("float");

                    b.Property<double>("Nominal")
                        .HasColumnType("float");

                    b.Property<double>("PriceInEuro")
                        .HasColumnType("float");

                    b.Property<string>("SecurityGuid")
                        .HasColumnType("nvarchar(450)");

                    b.Property<int>("TransactionType")
                        .HasColumnType("int");

                    b.HasKey("Guid");

                    b.HasIndex("SecurityGuid");

                    b.ToTable("Transactions");
                });

            modelBuilder.Entity("Aspenlaub.Net.GitHub.CSharp.Fundamental.Model.Entities.Holding", b =>
                {
                    b.HasOne("Aspenlaub.Net.GitHub.CSharp.Fundamental.Model.Entities.Security", "Security")
                        .WithMany("Holdings")
                        .HasForeignKey("SecurityGuid");

                    b.Navigation("Security");
                });

            modelBuilder.Entity("Aspenlaub.Net.GitHub.CSharp.Fundamental.Model.Entities.Quote", b =>
                {
                    b.HasOne("Aspenlaub.Net.GitHub.CSharp.Fundamental.Model.Entities.Security", "Security")
                        .WithMany("Quotes")
                        .HasForeignKey("SecurityGuid");

                    b.Navigation("Security");
                });

            modelBuilder.Entity("Aspenlaub.Net.GitHub.CSharp.Fundamental.Model.Entities.Transaction", b =>
                {
                    b.HasOne("Aspenlaub.Net.GitHub.CSharp.Fundamental.Model.Entities.Security", "Security")
                        .WithMany("Transactions")
                        .HasForeignKey("SecurityGuid");

                    b.Navigation("Security");
                });

            modelBuilder.Entity("Aspenlaub.Net.GitHub.CSharp.Fundamental.Model.Entities.Security", b =>
                {
                    b.Navigation("Holdings");

                    b.Navigation("Quotes");

                    b.Navigation("Transactions");
                });
#pragma warning restore 612, 618
        }
    }
}
