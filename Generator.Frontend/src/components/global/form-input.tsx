import * as React from 'react';
import type { Control, FieldValues, Path } from 'react-hook-form';
import { Controller } from 'react-hook-form';
import { Field, FieldError, FieldLabel } from '@/components/ui/field';
import { Input } from '@/components/ui/input';

interface FormInputProps<TFieldValues extends FieldValues> extends Omit<React.ComponentProps<'input'>, 'name'> {
  name: Path<TFieldValues>;
  control: Control<TFieldValues>;
  label?: string;
  fieldClassName?: string;
}

export default function FormInput<TFieldValues extends FieldValues>({
  name,
  control,
  label,
  id,
  fieldClassName,
  ...props
}: FormInputProps<TFieldValues>) {
  const inputId = id ?? `form-input-${name.toString()}`;
  return (
    <Controller
      name={name}
      control={control}
      render={({ field, fieldState }) => (
        <Field data-invalid={fieldState.invalid} className={fieldClassName}>
          {label && <FieldLabel htmlFor={inputId}>{label}</FieldLabel>}
          <Input
            {...field}
            {...props}
            id={inputId}
            aria-invalid={fieldState.invalid}
            value={(field.value ?? '') as string | number}
          />
          {fieldState.invalid && <FieldError errors={[fieldState.error]} />}
        </Field>
      )}
    />
  );
}
