namespace PayItOff.Domain.Entities
{
	public class User
	{
		public int Id { get; private set; }
		public string Email { get; private set; } = null!;
		public string PassHash { get; private set; } = null!;
		public string Nickname { get; private set; } = null!;
		public string Name { get; private set; } = null!;
		public string Surname { get; private set; } = null!;
		public string? AvatarUrl { get; private set; }
		public string? PhoneNumber { get; private set; }
		public string? IBAN { get; private set; }
		public NotificationsSettings NotificationsSettings { get; private set; } = null!;
		public string? VerificationToken { get; private set; }
		public DateTime? VerifiedAt { get; private set; }
		public string? PasswordResetToken { get; private set; }
		public DateTime? ResetTokenExpiresAt { get; private set; }
		public string? NewEmailPending { get; private set; }
		public string? EmailChangeToken { get; private set; }
		public bool IsActive { get; private set; }
		public bool IsVerified { get; private set; }
		public DateTime CreatedAt { get; private set; }
		public DateTime UpdatedAt { get; private set; }
		public DateTime? DeletedAt { get; private set; }

		protected User() { }

		private User(string email, string passHash, string nickname, string name, string surname, string avatarUrl, NotificationsSettings notificationsSettings, string? phoneNumber, string? iban)
		{
			if (string.IsNullOrWhiteSpace(email)) { throw new ArgumentException(nameof(email)); }
			if (string.IsNullOrWhiteSpace(passHash)) { throw new ArgumentException(nameof(passHash)); }
			if (string.IsNullOrWhiteSpace(nickname)) { throw new ArgumentException(nameof(nickname)); }
			if (string.IsNullOrWhiteSpace(name)) { throw new ArgumentException(nameof(name)); }
			if (string.IsNullOrWhiteSpace(surname)) { throw new ArgumentException(nameof(surname)); }
			Email = email;
			PassHash = passHash;
			Nickname = nickname;
			Name = name;
			Surname = surname;
			AvatarUrl = string.IsNullOrWhiteSpace(avatarUrl) ? "default-avatar.png" : avatarUrl;
			NotificationsSettings = notificationsSettings ?? new NotificationsSettings();
			VerificationToken = Guid.NewGuid().ToString();
			PhoneNumber = string.IsNullOrWhiteSpace(phoneNumber) ? null : phoneNumber;
			IBAN = string.IsNullOrWhiteSpace(iban) ? null : iban;
			CreatedAt = DateTime.UtcNow;
			UpdatedAt = DateTime.UtcNow;
			IsActive = true;
			IsVerified = false;
		}
		public static User Register(string email, string passHash, string nickname, string name, string surname, string? avatarUrl, string? phoneNumber, string? iban)
		{
			return new User(email, passHash, nickname, name, surname, avatarUrl!, new NotificationsSettings(), phoneNumber, iban);
		}

		public void ConfirmVerification(string token)
		{
			if (string.IsNullOrWhiteSpace(token)) { throw new ArgumentException("Invalid token."); }
			if (VerificationToken != token) { throw new Exception("Invalid verification token."); }

			IsVerified = true;
			VerifiedAt = DateTime.UtcNow;
			VerificationToken = null;
			UpdatedAt = DateTime.UtcNow;
		}

		public void UpdateNotifications(NotificationsSettings newSettings)
		{
			if (newSettings == null) { throw new ArgumentNullException(nameof(newSettings)); }

			NotificationsSettings = newSettings;
			UpdatedAt = DateTime.UtcNow;
		}

		public void UpdateInfo(string nickname, string name, string surname, string? phoneNumber, string? iban)
		{
			if (string.IsNullOrWhiteSpace(nickname)) { throw new ArgumentException(nameof(nickname)); }
			if (string.IsNullOrWhiteSpace(name)) { throw new ArgumentException(nameof(name)); }
			if (string.IsNullOrWhiteSpace(surname)) { throw new ArgumentException(nameof(surname)); }

			Nickname = nickname;
			Name = name;
			Surname = surname;

			PhoneNumber = string.IsNullOrWhiteSpace(phoneNumber) ? null : phoneNumber;
			IBAN = string.IsNullOrWhiteSpace(iban) ? null : iban;

			UpdatedAt = DateTime.UtcNow;
		}

		public void UpdateAvatar(string avatarUrl)
		{
			AvatarUrl = string.IsNullOrWhiteSpace(avatarUrl) ? "default-avatar.png" : avatarUrl;

			UpdatedAt = DateTime.UtcNow;
		}

		public void Delete()
		{
			IsActive = false;
			UpdatedAt = DateTime.UtcNow;
			DeletedAt = DateTime.UtcNow;
		}

		public void GeneratePasswordResetToken()
		{
			PasswordResetToken = Guid.NewGuid().ToString();
			ResetTokenExpiresAt = DateTime.UtcNow.AddHours(1);
			UpdatedAt = DateTime.UtcNow;
		}

		public void ResetPassword(string token, string newPassHash)
		{
			if (string.IsNullOrWhiteSpace(newPassHash)) { throw new ArgumentException("Invalid new Password."); }
			if (string.IsNullOrWhiteSpace(token)) { throw new ArgumentException("Invalid token."); }
			if (PasswordResetToken != token) { throw new Exception("Invalid reset token."); }
			if (ResetTokenExpiresAt < DateTime.UtcNow) { throw new Exception("Token expired."); }

			PassHash = newPassHash;
			PasswordResetToken = null;
			ResetTokenExpiresAt = null;
			UpdatedAt = DateTime.UtcNow;
		}

		public void ModifyPassword(string newPassHash)
		{
			if (string.IsNullOrWhiteSpace(newPassHash)) { throw new ArgumentException("Invalid new Password."); }

			PassHash = newPassHash;
			UpdatedAt = DateTime.UtcNow;
		}

		public void GenerateEmailChangeToken(string newEmail)
		{
			if (string.IsNullOrWhiteSpace(newEmail)) { throw new ArgumentException("Invalid new Email."); }
			EmailChangeToken = Guid.NewGuid().ToString();
			NewEmailPending = newEmail;
			UpdatedAt = DateTime.UtcNow;
		}

		public void EmailChange(string token)
		{
			if (string.IsNullOrWhiteSpace(token)) { throw new ArgumentException("Invalid token."); }
			if (EmailChangeToken != token) { throw new Exception("Invalid change token."); }
			if (string.IsNullOrWhiteSpace(NewEmailPending)) { throw new ArgumentException("Invalid new Email."); }

			Email = NewEmailPending;
			NewEmailPending = null;
			EmailChangeToken = null;
			UpdatedAt = DateTime.UtcNow;
		}
	}
	public record NotificationsSettings(bool ReceiveEmail = true, bool DailySummary = false, bool NotifyOnGroupJoined = true, bool NotifyOnExpenseAdded = true, bool NotifyOnGroupRemoved = true, bool NotifyOnFriendRemoved = true, bool NotifyOnExpenseChanged = true, bool NotifyOnTransferConfirmed = true);
}