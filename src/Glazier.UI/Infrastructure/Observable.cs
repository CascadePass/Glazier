using System.Collections.Generic;
using System.ComponentModel;

namespace CascadePass.Glazier.UI
{
    /// <summary>
    /// Base class for objects implementing <see cref="INotifyPropertyChanged"/>.
    /// </summary>
    public abstract class Observable
    {
        /// <summary>
        /// Event raised when the value of a property changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Sets the value of a property, and raises the <see cref="PropertyChanged"/>
        /// event if the value has been updated.
        /// </summary>
        /// <typeparam name="T">The type of the property.</typeparam>
        /// <param name="field">The backing field whose value to test and possibly change.</param>
        /// <param name="value">The new value of the property.</param>
        /// <param name="propertyName">The name of the property, to raise the <see cref="PropertyChanged"/> event.</param>
        /// <returns>True if the value has changed, false if it was already the new value.</returns>
        protected bool SetPropertyValue<T>(ref T field, T value, string propertyName)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
            {
                return false;
            }

            field = value;
            this.OnPropertyChanged(propertyName);
            return true;
        }

        /// <summary>
        /// Sets the value of a property, and raises the <see cref="PropertyChanged"/>
        /// event if the value has been updated.
        /// </summary>
        /// <typeparam name="T">The type of the property.</typeparam>
        /// <param name="field">The backing field whose value to test and possibly change.</param>
        /// <param name="value">The new value of the property.</param>
        /// <param name="propertyName">The name of the properties, to raise the <see cref="PropertyChanged"/> event for.</param>
        /// <returns>True if the value has changed, false if it was already the new value.</returns>
        protected bool SetPropertyValue<T>(ref T field, T value, string[] propertyNames)
        {
            if (!EqualityComparer<T>.Default.Equals(field, value))
            {
                field = value;

                foreach (string propertyName in propertyNames)
                {
                    this.OnPropertyChanged(propertyName);
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// Raises the <see cref="PropertyChanged"/> event.
        /// </summary>
        /// <param name="propertyName">The name of the property whose
        /// value has changed.</param>
        protected virtual void OnPropertyChanged(string propertyName)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
