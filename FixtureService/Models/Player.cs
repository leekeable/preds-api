namespace FixtureService.Models
{
    public class Player
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Player" /> class.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <param name="name">The name.</param>
        /// <param name="nickName">Name of the nick.</param>
        /// <param name="email">The email.</param>
        /// <param name="isAdmin">if set to <c>true</c> [is admin].</param>
        public Player(int id, string name, string nickName, string email, bool isAdmin)
        {
            NickName = nickName;
            Id = id;
            IsAdmin = isAdmin;
            Email = email;
            Name = name;
        }

        /// <summary>
        /// Gets or sets the first name.
        /// </summary>
        /// <value>
        /// The first name.
        /// </value>
        public string Name { get; }

        /// <summary>
        /// Gets or sets the nick name.
        /// </summary>
        /// <value>
        /// The nick name.
        /// </value>
        public string NickName { get; }


        /// <summary>
        /// Gets or sets the email.
        /// </summary>
        /// <value>
        /// The email.
        /// </value>
        public string Email { get; }

        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>
        /// The id.
        /// </value>
        public int Id { get; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is an administrator.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is an administrator; otherwise, <c>false</c>.
        /// </value>
        public bool IsAdmin { get; }

        public override string ToString()
        {
            return NickName;
        }

    }
}
