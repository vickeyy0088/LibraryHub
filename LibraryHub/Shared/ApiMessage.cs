namespace LibraryHub.Shared
{
    public class ApiMessage
    {
        public static class Auth
        {
            public const string RegisterSuccess = "User registered successfully.";
            public const string RegistrationFailed = "User registration failed.";
            public const string EmailAlreadyExists = "This email is already registered.";
            public const string InvalidRole = "Invalid role. Allowed roles are Member and Librarian.";
            public const string RoleNotFound = "The selected role is not configured.";
            public const string LoginSuccess = "Login successful.";
            public const string InvalidCredentials = "Invalid email or password.";
            public const string RefreshTokenGenerated = "Tokens refreshed successfully.";
            public const string InvalidRefreshToken = "Invalid or expired refresh token.";
            public const string LogoutSuccess = "Logout successful.";
            public const string PasswordChanged = "Password changed successfully.";
            public const string PasswordChangeFailed = "Password change failed.";
            public const string GetUserData = "User profile retrieved successfully.";
            public const string UserNotFound = "User not found.";
            public const string SystemHealthy = "System is healthy.";
            public const string SystemUnhealthy = "System is unhealthy.";

        }

        public static class Common
        {
            public const string OperationSuccess = "Operation completed successfully.";
            public const string OperationFailed = "Operation failed.";
            public const string DataNotFound = "No data found.";
            public const string InvalidId = "Invalid ID.";
            public const string InvalidPaginationParameters = "Page number and page size must be greater than zero.";
            public const string InvalidRequest = "Invalid request.";
            public const string ValidationFailed = "Validation failed.";
            public const string Unauthorized = "You are not authorized to perform this action.";
            public const string ServerError = "An unexpected error occurred.";
        }

        public static class Books
        {
            public const string Created = "Book created successfully.";
            public const string Updated = "Book updated successfully.";
            public const string Deleted = "Book deleted successfully.";
            public const string GetById = "Book retrieved successfully.";
            public const string GetAll = "Books retrieved successfully.";
            public const string NotFound = "Book not found.";
            public const string ISBNExists = "A book with this ISBN already exists.";
            public const string TitleISBNRequired = "Book title and ISBN are required.";
            //public const string UpdateFailed = "Failed to update book.";
            //public const string PriceInvalid = "Book price cannot be negative.";
            public const string TotalCopiesInvalid = "Total copies cannot be negative.";
            public const string AvailableCopiesExceedTotal = "Available copies cannot exceed total copies.";
            public const string AuthorNotFound = "The specified author does not exist.";
            public const string CategoryNotFound = "The specified category does not exist.";
            public const string HasLoans = "This book cannot be deleted because loan records exist.";

        }

        public static class Authors
        {
            public const string Created = "Author created successfully.";
            public const string Updated = "Author updated successfully.";
            public const string Deleted = "Author deleted successfully.";
            public const string GetById = "Author retrieved successfully.";
            public const string GetAll = "Authors retrieved successfully.";
            public const string NotFound = "Author not found.";
            public const string FullNameIsRequired = "Author's full name is required.";
        }

        public static class Categories
        {
            public const string Created = "Category created successfully.";
            public const string Updated = "Category updated successfully.";
            public const string Deleted = "Category deleted successfully.";
            public const string GetById = "Category retrieved successfully.";
            public const string GetAll = "Categories retrieved successfully.";
            public const string NotFound = "Category not found.";
            public const string NameIsRequired = "Category name is required.";
            public const string AlreadyExists = "A category with this name already exists.";
        }

        public static class Members
        {
            public const string Created = "Member created successfully.";
            public const string Updated = "Member updated successfully.";
            public const string Deleted = "Member deleted successfully.";
            public const string GetById = "Member retrieved successfully.";
            public const string GetAll = "Members retrieved successfully.";
            public const string NotFound = "Member not found.";
            public const string InvalidApplicationUserId = "The specified application user does not exist.";
            public const string InvalidMembershipNumber = "Membership number is required.";
            public const string AlreadyMember = "This user already has a membership.";
            public const string MembershipNumberExists = "Membership number already exists.";
            public const string HasLoans = "This member cannot be deleted because they have loan records.";
        }

        public static class Loans
        {
            public const string Created = "Loan created successfully.";
            public const string GetAll = "Loans retrieved successfully.";
            public const string GetOverdueLoans = "Overdue loans retrieved successfully.";
            public const string Returned = "Book returned successfully.";
            public const string NotFound = "Loan not found.";
            public const string CreateFailed = "Failed to create loan.";
            public const string DueDateError = "Due date must be in the future.";
            public const string CopiesError = "No available copies of the book to loan.";
            public const string UserIdMissing = "User authentication is required.";
            public const string AlreadyReturned = "This loan has already been returned.";
        }

        public static class Reports
        {
            public const string PopularBooks = "Popular books report retrieved successfully.";
            public const string MemberActivity = "Member activity report retrieved successfully.";
            public const string InvalidLimit = "Invalid limit specified.";
        }

        public static class Users
        {
            public const string GetAll = "Users retrieved successfully.";
            public const string RoleAssigned = "Role assigned successfully.";
            public const string RoleRemoved = "Role removed successfully.";
            public const string NotFound = "User not found.";
            public const string AlreadyHasRole = "User already has this role.";
            public const string RoleNotFound = "Role not found.";
        }

    }
}
