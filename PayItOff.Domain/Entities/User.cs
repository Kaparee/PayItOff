namespace PayItOff.Domain.Entities
{
	public class User
	{
		public int Id { get; private set; }
		public string Email { get; private set; }
		public string PassHash { get; private set; }
		public string Nickname { get; private set; }
		public string Name { get; private set; }
		public string Surname { get; private set; }
		public string AvatarUrl { get; private set; }
		public string? PhoneNumber { get; private set; }
		public string? IBAN { get; private set; }
		public NotificationsSettings NotificationsSettings { get; private set; }
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
			if (string.IsNullOrWhiteSpace(avatarUrl)) { throw new ArgumentException(nameof(avatarUrl)); }
			Email = email;
			PassHash = passHash;
			Nickname = nickname;
			Name = name;
			Surname = surname;
			AvatarUrl = avatarUrl;
			NotificationsSettings = notificationsSettings ?? new NotificationsSettings();
			VerificationToken = Guid.NewGuid().ToString();
			PhoneNumber = string.IsNullOrWhiteSpace(phoneNumber) ? null : phoneNumber;
			IBAN = string.IsNullOrWhiteSpace(iban) ? null : iban;
			CreatedAt = DateTime.UtcNow;
			UpdatedAt = DateTime.UtcNow;
			IsActive = true;
			IsVerified = false;
		}
		public static User Register(string email, string passHash, string nickname, string name, string surname, string? phoneNumber, string? iban)
		{
			string avatarUrl = "default_avatar.jpg";
			return new User(email, passHash, nickname, name, surname, avatarUrl, new NotificationsSettings(), phoneNumber, iban);
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

		public void UpdateNotifications(bool? receiveEmail = null, bool? dailySummary = null, bool? notifyOnGroupJoined = null, bool? notifyOnExpenseAdded = null, bool? notifyOnGroupRemoved = null, bool? notifyOnFriendRemoved = null, bool? notifyOnExpenseChanged = null, bool? notifyOnTransferConfirmed = null)
		{
			NotificationsSettings = NotificationsSettings with
			{
				ReceiveEmail = receiveEmail ?? NotificationsSettings.ReceiveEmail,
				DailySummary = dailySummary ?? NotificationsSettings.DailySummary,
				NotifyOnGroupJoined = notifyOnGroupJoined ?? NotificationsSettings.NotifyOnGroupJoined,
				NotifyOnExpenseAdded = notifyOnExpenseAdded ?? NotificationsSettings.NotifyOnExpenseAdded,
				NotifyOnGroupRemoved = notifyOnGroupRemoved ?? NotificationsSettings.NotifyOnGroupRemoved,
				NotifyOnFriendRemoved = notifyOnFriendRemoved ?? NotificationsSettings.NotifyOnFriendRemoved,
				NotifyOnExpenseChanged = notifyOnExpenseChanged ?? NotificationsSettings.NotifyOnExpenseChanged,
				NotifyOnTransferConfirmed = notifyOnTransferConfirmed ?? NotificationsSettings.NotifyOnTransferConfirmed
			};

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
			if (string.IsNullOrWhiteSpace(avatarUrl)) { throw new ArgumentException(nameof(avatarUrl)); }

			AvatarUrl = avatarUrl;

			UpdatedAt = DateTime.UtcNow;
		}

		public void UpdatePhoneNumber(string? phoneNumber)
		{
			PhoneNumber = string.IsNullOrWhiteSpace(phoneNumber) ? null : phoneNumber;

			UpdatedAt = DateTime.UtcNow;
		}

		public void UpdateIBAN(string? iban)
		{
			IBAN = string.IsNullOrWhiteSpace(iban) ? null : iban;

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