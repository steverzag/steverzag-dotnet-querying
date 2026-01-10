using Bogus;
using ExpDataWorker.Data.Enums;
using ExpDataWorker.Data.Models;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;

namespace ExpDataWorker.Data
{
	public class AppDbContext : DbContext
	{
		//public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

		public DbSet<AppPerson> Persons { get; set; }
		public DbSet<Address> Addresses { get; set; }
		public DbSet<Phone> Phones { get; set; }
		public DbSet<Affiliation> Affilations { get; set; }
		public DbSet<AffiliationPersonMap> AffiliationPersonMaps { get; set; }
		public DbSet<FormerJob> Jobs { get; set; }
		public DbSet<TaxInfo> TaxInfos { get; set; }

		override protected void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
		{
			optionsBuilder.UseSqlServer("Server=localhost;Database=ExpDataWorkerBetaDb;Integrated Security=True;TrustServerCertificate=True;")
				.UseSeeding((context, _) =>
				{
					var containsPerson = context.Set<AppPerson>().Any();

					if (!containsPerson)
						SeedData(context);
				});
		}

		override protected void OnModelCreating(ModelBuilder modelBuilder)
		{
			modelBuilder.Entity<AppPerson>(entity =>
			{
				entity.HasKey(e => e.Id);
				entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
				entity.Property(e => e.Email).IsRequired().HasMaxLength(200);
				entity.Property(e => e.JobTitle).HasMaxLength(100);
				entity.Property(e => e.DateOfBirth).IsRequired();
			});

			modelBuilder.Entity<Address>(entity =>
			{
				entity.HasKey(e => e.Id);
				entity.Property(e => e.AddressLine).IsRequired().HasMaxLength(300);
				entity.Property(e => e.City).IsRequired().HasMaxLength(100);
				entity.Property(e => e.State).IsRequired().HasMaxLength(100);
				entity.Property(e => e.ZipCode).IsRequired().HasMaxLength(20);
			});

			modelBuilder.Entity<Phone>(entity =>
			{
				entity.HasKey(e => e.Id);
				entity.Property(e => e.CountryCode).HasColumnType("char(10)");
				entity.Property(e => e.Number).HasColumnType("char(20)");
			});

			modelBuilder.Entity<Affiliation>(entity =>
			{
				entity.HasKey(e => e.Id);
			});

			modelBuilder.Entity<AffiliationPersonMap>(entity =>
			{
				entity.HasKey(e => new { e.AffiliationId, e.PersonId });
				entity.HasOne(apm => apm.Person)
				.WithMany(p => p.Affiliations)
				.HasForeignKey(apm => apm.PersonId)
				.OnDelete(DeleteBehavior.Cascade);
				entity.HasOne(apm => apm.Affiliation)
				.WithMany(p => p.AffiliatedPeople)
				.HasForeignKey(apm => apm.AffiliationId)
				.OnDelete(DeleteBehavior.Cascade);
			});

			modelBuilder.Entity<TaxInfo>(entity =>
			{
				entity.HasKey(e => e.Id);
				entity.HasOne(e => e.Person)
					.WithOne(e => e.TaxInfo)
					.HasForeignKey<TaxInfo>(e => e.PersonId);
				entity.Property(e => e.LastDeclarationAmmount)
					.HasColumnType("decimal(18,2)");
				entity.Property(e => e.EstimatedNextDeclarationAmmount)
					.HasColumnType("decimal(18,2)");
			});

			modelBuilder.Entity<FormerJob>(entity =>
			{
				entity.HasKey(e => e.Id);
				entity.HasOne(e => e.Person)
					.WithOne(e => e.Job)
					.HasForeignKey<FormerJob>(e => e.PersonId);
			});

			modelBuilder.Entity<Address>().HasOne(e => e.Person)
				.WithOne(e => e.Address)
				.HasForeignKey<Address>(e => e.PersonId);

			modelBuilder.Entity<Phone>().HasOne(e => e.Person)
				.WithMany(e => e.Phones)
				.HasForeignKey(e => e.PersonId)
				.OnDelete(DeleteBehavior.Cascade);
		}

		private static void SeedData(DbContext context)
		{
			Randomizer.Seed = new Random(8675309);

			var now = DateTime.Now;

			var personFaker = new Faker<AppPerson>()
				.RuleFor(p => p.Name, (f, p) => f.Person.FullName)
				.RuleFor(p => p.DateOfBirth, (f, p) => f.Person.DateOfBirth)
				.RuleFor(p => p.JobTitle, (f, p) => f.OrNull(f, .2f)?.Name?.JobTitle())
				.RuleFor(p => p.Email, (f, p) => f.Person.Email)
				.RuleFor(p => p.MaritalStatus, (f, p) => now.Year - f.Person.DateOfBirth.Year < 18 ? null : f.PickRandom<MaritalStatus>())
				.RuleFor(p => p.Gender, (f, p) => f.Person.Gender == Bogus.DataSets.Name.Gender.Male ? Gender.Male : Gender.Female);

			var addressFaker = new Faker<Address>()
				.RuleFor(a => a.AddressLine, (f, a) => f.Address.StreetAddress())
				.RuleFor(a => a.City, (f, a) => f.Address.City())
				.RuleFor(a => a.State, (f, a) => f.Address.State())
				.RuleFor(a => a.ZipCode, (f, a) => f.Address.ZipCode());

			var phoneFaker = new Faker<Phone>()
				.RuleFor(p => p.CountryCode, (f, p) => f.OrNull(f, .4f).RandomPhoneCountry())
				.RuleFor(p => p.PhoneType, (f, p) => f.RandomPhoneType())
				.RuleFor(p => p.IsOptedIn, (f, p) => f.OrNull(f, .3f)?.Random?.Bool(.6f))
				.RuleFor(p => p.Number, (f, p) => f.Phone.PhoneNumber("(###) ###-####"));

			var affiliationFaker = new Faker<Affiliation>()
				.RuleFor(a => a.Name, (f, a) => f.Company.CompanyName())
				.RuleFor(a => a.Description, (f, a) => f.Lorem.Sentence())
				.RuleFor(a => a.CreatedOn, (f, a) => f.Date.BetweenDateOnly(DateOnly.FromDateTime(DateTime.Now.AddYears(-10)), DateOnly.FromDateTime(DateTime.Now.AddYears(-2))))
				.RuleFor(a => a.NumberOfMembers, (f, a) => f.OrNull(f, .3f)?.Random?.Int(1, 10000));

			var jobsFaker = new Faker<FormerJob>()
				.RuleFor(j => j.CompanyName, (f, j) => f.Company.CompanyName())
				.RuleFor(j => j.Address, (f, j) => f.Address.FullAddress())
				.RuleFor(j => j.MonthlySalary, (f, j) => (double)f.Finance.Amount(2000, 20000))
				.RuleFor(j => j.SinceDate, (f, j) => f.Date.BetweenDateOnly(DateOnly.FromDateTime(DateTime.Now.AddYears(-10)), DateOnly.FromDateTime(DateTime.Now.AddYears(-1))))
				.RuleFor(j => j.ToDate, (f, j) => f.OrNull(f, .5f)?.Date?.BetweenDateOnly(DateOnly.FromDateTime(DateTime.Now.AddYears(-1)), DateOnly.FromDateTime(DateTime.Now)))
				.RuleFor(j => j.StartHour, (f, j) => new TimeOnly(f.Random.Int(6, 10), 0))
				.RuleFor(j => j.EndHour, (f, j) => f.OrNull(f, .4f) != null ? new TimeOnly(f.Random.Int(16, 20), 0) : null)
				.RuleFor(j => j.NumberOfEmployees, (f, j) => (short)f.Random.Int(5, 1000));

			var taxInfoFaker = new Faker<TaxInfo>()
				.RuleFor(t => t.TaxType, (f, t) => f.PickRandom<TaxType>())
				.RuleFor(t => t.TaxRate, (f, t) => f.Random.Double(0.1, 0.35))
				.RuleFor(t => t.Country, (f, t) => f.Address.Country())
				.RuleFor(t => t.LastDeclarationDate, (f, t) => f.Date.BetweenDateOnly(DateOnly.FromDateTime(DateTime.Now.AddYears(-2)), DateOnly.FromDateTime(DateTime.Now)))
				.RuleFor(t => t.LastDeclarationAmmount, (f, t) => f.Finance.Amount(1000, 100000))
				.RuleFor(t => t.EstimatedNextDeclarationAmmount, (f, t) => f.OrNull(f, .4f)?.Finance?.Amount(1000, 100000));

			var persons = personFaker.Generate(10_000);
			var addresses = addressFaker.Generate(10_000);
			var phones = phoneFaker.Generate(25_000);
			var affiliations = affiliationFaker.Generate(200);
			var jobs = jobsFaker.Generate(10_000);
			var taxInfos = taxInfoFaker.Generate(10_000);

			var skipPhones = 0;

			foreach (var affiliation in affiliations)
			{
				affiliation.WorkingHours = [.. FakerExtensions.GetRandomWorkingHours()];
			}

			for (int i = 0; i < persons.Count; i++)
			{
				persons[i].Address = addresses[i];
				persons[i].Job = jobs[i];
				persons[i].TaxInfo = taxInfos[i];

				var addAffiliation = new Random().NextDouble() >= .5;
				if (addAffiliation)
				{
					var affiliationIndex = (int)new Random().NextInt64(0, affiliations.Count);

					var pAge = DateTime.Now.Year - persons[i].DateOfBirth.Year;
					var randomNYears = (int)new Random().NextInt64(pAge / 2, pAge);
					var randomNDays = new Random().NextInt64(0, 200);
					persons[i].Affiliations = [
						new ()
						{
							AffiliatedAt = DateTime.Now.AddYears(-randomNYears).AddDays(randomNDays),
							AffiliationType = (AffiliationType)(int)new Random().NextInt64(0, 5),
							Affiliation = affiliations[affiliationIndex]
						}
					];
				}

				var addPhonesCount = new Random().NextDouble() >= .2;
				if (addPhonesCount && skipPhones < phones.Count)
				{
					var phonesCount = (int)new Random().NextInt64(1, 3);
					persons[i].Phones = phones.Skip(skipPhones).Take(phonesCount).ToList();
					skipPhones += phonesCount;
				}
			}

			context.Set<AppPerson>().AddRange(persons);
			context.SaveChanges();
		}
	}

	public static class FakerExtensions
	{
		public static string? RandomPhoneCountry(this Faker f)
		{
			if (f is null)
				return null;

			var countryCodes = new string[] { "+1", "+52", "+32", "+34", "+55" };
			var index = new Random().NextInt64(0, countryCodes.Length);

			return countryCodes[index];
		}

		public static PhoneType? RandomPhoneType(this Faker f)
		{
			var countryTypes = Enum.GetValues<PhoneType>();
			var index = new Random().NextInt64(0, countryTypes.Length);

			return countryTypes[index];
		}

		public static IEnumerable<WorkingHours> GetRandomWorkingHours()
		{
			var workingHoursList = new List<WorkingHours>();
			var daysOfWeek = Enum.GetValues<DayOfWeek>();

			var openTimes = new TimeOnly[]
			{
				new (6, 0),
				new (7, 0),
				new (8, 0),
				new (9, 0),
				new (10, 0)
			};

			var closeTimes = new TimeOnly[]
			{
				new (16, 0),
				new (17, 0),
				new (18, 0),
				new (19, 0),
				new (20, 0)
			};

			var monday = new WorkingHours
			{
				Day = DayOfWeek.Monday,
				OpenTime = openTimes[new Random().Next(0, openTimes.Length)],
				CloseTime = closeTimes[new Random().Next(0, closeTimes.Length)]
			};
			var tuesday = new WorkingHours
			{
				Day = DayOfWeek.Tuesday,
				OpenTime = openTimes[new Random().Next(0, openTimes.Length)],
				CloseTime = closeTimes[new Random().Next(0, closeTimes.Length)]
			};
			var wednesday = new WorkingHours
			{
				Day = DayOfWeek.Wednesday,
				OpenTime = openTimes[new Random().Next(0, openTimes.Length)],
				CloseTime = closeTimes[new Random().Next(0, closeTimes.Length)]
			};
			var thursday = new WorkingHours
			{
				Day = DayOfWeek.Thursday,
				OpenTime = openTimes[new Random().Next(0, openTimes.Length)],
				CloseTime = closeTimes[new Random().Next(0, closeTimes.Length)]
			};
			var friday = new WorkingHours
			{
				Day = DayOfWeek.Friday,
				OpenTime = openTimes[new Random().Next(0, openTimes.Length)],
				CloseTime = closeTimes[new Random().Next(0, closeTimes.Length)]
			};
			var saturday = new WorkingHours
			{
				Day = DayOfWeek.Saturday,
				OpenTime = openTimes[new Random().Next(0, openTimes.Length)],
				CloseTime = closeTimes[new Random().Next(0, closeTimes.Length)]
			};
			var sunday = new WorkingHours
			{
				Day = DayOfWeek.Sunday,
				OpenTime = openTimes[new Random().Next(0, openTimes.Length)],
				CloseTime = closeTimes[new Random().Next(0, closeTimes.Length)]
			};

			var conbinations = new List<WorkingHours>[]
			{
				[monday, tuesday, wednesday, thursday, friday],
				[monday, tuesday, wednesday, thursday, friday, saturday],
				[monday, tuesday, wednesday, thursday, friday, saturday, sunday],
				[monday, wednesday, friday],
				[tuesday, thursday],
				[saturday, sunday],
				[friday, saturday, sunday]
			};

			return conbinations[new Random().Next(0, conbinations.Length)];
		}
	}
}
