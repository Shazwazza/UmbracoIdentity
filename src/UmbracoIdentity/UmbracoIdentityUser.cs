using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using UmbracoIdentity.Models;

namespace UmbracoIdentity
{
    public class UmbracoIdentityUser : IdentityUser<int, IdentityUserLogin<int>, IdentityUserRole<int>, IdentityUserClaim<int>>
    {
        /// <summary>
        /// Gets/sets the members real name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Overridden to make the retrieval lazy
        /// </summary>
        public override ICollection<IdentityUserLogin<int>> Logins
        {
            get
            {
                if (_getLogins != null && !_getLogins.IsValueCreated)
                {
                    _logins = new ObservableCollection<IdentityUserLogin<int>>();
                    foreach (var l in _getLogins.Value)
                    {
                        _logins.Add(l);
                    }
                    //now assign events
                    _logins.CollectionChanged += Logins_CollectionChanged;
                }
                return _logins;
            }
        }

        public bool LoginsChanged { get; private set; }

        void Logins_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            LoginsChanged = true;
        }

        private ObservableCollection<IdentityUserLogin<int>> _logins;
        private Lazy<IEnumerable<IdentityUserLogin<int>>> _getLogins;

        /// <summary>
        /// Used to set a lazy call back to populate the user's Login list
        /// </summary>
        /// <param name="callback"></param>
        public void SetLoginsCallback(Lazy<IEnumerable<IdentityUserLogin<int>>> callback)
        {
            if (callback == null) throw new ArgumentNullException("callback");
            _getLogins = callback;
        }
    }
}