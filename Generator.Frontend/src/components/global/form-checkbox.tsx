import type { Control, FieldValues, Path } from 'react-hook-form';
import { Controller } from 'react-hook-form';
import { Field, FieldError, FieldLabel } from '@/components/ui/field';
import { Checkbox } from '@/components/ui/checkbox';

interface FormCheckboxProps<TFieldValues extends FieldValues> {
  name: Path<TFieldValues>;
  control: Control<TFieldValues>;
  label?: string;
  id?: string;
  disabled?: boolean;
  className?: string;
  onCheckedChange?: (checked: boolean) => void;
}

export default function FormCheckbox<TFieldValues extends FieldValues>({
  name,
  control,
  label,
  id,
  disabled,
  className,
  onCheckedChange
}: FormCheckboxProps<TFieldValues>) {
  const checkboxId = id ?? `form-checkbox-${name.toString()}`;
  return (
    <Controller
      name={name}
      control={control}
      render={({ field, fieldState }) => (
        <Field data-invalid={fieldState.invalid} orientation='horizontal' className={className}>
          <Checkbox
            id={checkboxId}
            checked={!!field.value}
            onCheckedChange={checked => {
              field.onChange(checked);
              if (onCheckedChange) {
                onCheckedChange(!!checked);
              }
            }}
            disabled={disabled}
            aria-invalid={fieldState.invalid}
          />
          {label && (
            <FieldLabel htmlFor={checkboxId} className='font-normal'>
              {label}
            </FieldLabel>
          )}
          {fieldState.invalid && <FieldError errors={[fieldState.error]} />}
        </Field>
      )}
    />
  );
}
