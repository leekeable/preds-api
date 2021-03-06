﻿namespace FixtureService.Models
{
    public class Player
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Player" /> class.
        /// </summary>
        /// <param name="id">The id of the player.</param>
        /// <param name="name">The name of the player.</param>
        /// <param name="nickName">Nickname/username of the player.</param>
        /// <param name="email">The players email.</param>
        /// <param name="isAdmin">if set to <c>true</c> the player is an administrator.</param>
        public Player(int id, string name, string nickName, string email, bool isAdmin)
        {
            NickName = nickName;
            Id = id;
            IsAdmin = isAdmin;
            Email = email;
            Name = name;
        }

        /// <summary>
        /// Gets the name of the player.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public string Name { get; }

        /// <summary>
        /// Gets the nick name/username of the player.
        /// </summary>
        /// <value>
        /// The nick name.
        /// </value>
        public string NickName { get; }


        /// <summary>
        /// Gets the players email address.
        /// </summary>
        /// <value>
        /// The email.
        /// </value>
        public string Email { get; }

        /// <summary>
        /// Gets the players id.
        /// </summary>
        /// <value>
        /// The id.
        /// </value>
        public int Id { get; }

        /// <summary>
        /// Gets or sets a value indicating whether this player is an administrator.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this player is an administrator; otherwise, <c>false</c>.
        /// </value>
        public bool IsAdmin { get; }

        public override string ToString()
        {
            return NickName;
        }
    }
}
