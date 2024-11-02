﻿// <auto-generated />
using System;
using GrindSoft.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace GrindSoft.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20241029165301_InitialMigrate2")]
    partial class InitialMigrate2
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "8.0.10");

            modelBuilder.Entity("GrindSoft.Models.Message", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("AuthorId")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("Content")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("DateTime")
                        .HasColumnType("TEXT");

                    b.Property<int>("SessionId")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("SessionId");

                    b.ToTable("Messages");
                });

            modelBuilder.Entity("GrindSoft.Models.Session", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("AccessToken")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("AuthorId")
                        .HasColumnType("TEXT");

                    b.Property<string>("ChannelId")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<int>("DelayBetweenMessages")
                        .HasColumnType("INTEGER");

                    b.Property<string>("LastProcessedMessageId")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<int>("MessageCount")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Prompt")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("ServerId")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("Status")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("UserAgent")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("Sessions");
                });

            modelBuilder.Entity("GrindSoft.Models.Message", b =>
                {
                    b.HasOne("GrindSoft.Models.Session", null)
                        .WithMany("Messages")
                        .HasForeignKey("SessionId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("GrindSoft.Models.Session", b =>
                {
                    b.Navigation("Messages");
                });
#pragma warning restore 612, 618
        }
    }
}
