import { useId } from 'react'
import { bem } from '@/shared/bem'
import styles from './Field.module.scss'

const b = bem(styles)

/** Label, control, hint and error laid out together. `children(props)` receives the control's aria props. */
export function Field({ label, hint, error, required, optionalText, className, children }) {
  const id = useId()
  const hintId = hint ? `${id}-hint` : undefined
  const errorId = error ? `${id}-error` : undefined
  const describedBy = [hintId, errorId].filter(Boolean).join(' ') || undefined

  return (
    <div className={b('field', { invalid: Boolean(error) }, className)}>
      {label && (
        <label className={styles['field__label']} htmlFor={id}>
          {label}
          {!required && optionalText && <span className={styles['field__optional']}> ({optionalText})</span>}
        </label>
      )}
      {children({ id, 'aria-describedby': describedBy, 'aria-invalid': error ? true : undefined, required })}
      {hint && (
        <p id={hintId} className={styles['field__hint']}>
          {hint}
        </p>
      )}
      {error && (
        <p id={errorId} className={styles['field__error']}>
          {error}
        </p>
      )}
    </div>
  )
}

/** A labelled text input; `multiline` makes it a textarea. */
export function TextField({ label, hint, error, required, optionalText, className, multiline = false, ...inputProps }) {
  return (
    <Field label={label} hint={hint} error={error} required={required} optionalText={optionalText} className={className}>
      {(aria) =>
        multiline ? (
          <textarea className={b('field__control', { multiline: true })} rows={4} {...aria} {...inputProps} />
        ) : (
          <input className={styles['field__control']} {...aria} {...inputProps} />
        )
      }
    </Field>
  )
}

/**
 * A labelled native select. `options`: [{ value, label }], or groups [{ label, options: [{ value, label }] }] shown as
 * option groups; `placeholder` adds an empty first option.
 */
export function SelectField({ label, hint, error, required, optionalText, className, options, placeholder, ...selectProps }) {
  return (
    <Field label={label} hint={hint} error={error} required={required} optionalText={optionalText} className={className}>
      {(aria) => (
        <select className={b('field__control', { select: true })} {...aria} {...selectProps}>
          {placeholder !== undefined && <option value="">{placeholder}</option>}
          {options.map((option) =>
            option.options ? (
              <optgroup key={`group-${option.label}`} label={option.label}>
                {option.options.map((item) => (
                  <option key={item.value} value={item.value}>
                    {item.label}
                  </option>
                ))}
              </optgroup>
            ) : (
              <option key={option.value} value={option.value}>
                {option.label}
              </option>
            ),
          )}
        </select>
      )}
    </Field>
  )
}

/** A checkbox with its label to the right. */
export function CheckboxField({ label, className, ...inputProps }) {
  return (
    <label className={b('field__check', null, className)}>
      <input type="checkbox" className={styles['field__checkbox']} {...inputProps} />
      <span>{label}</span>
    </label>
  )
}
