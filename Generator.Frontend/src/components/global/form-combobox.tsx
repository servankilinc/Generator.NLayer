import type { Control, FieldValues, Path } from 'react-hook-form';
import { Controller } from 'react-hook-form';
import { Field, FieldError, FieldLabel } from '@/components/ui/field';
import { Combobox, ComboboxContent, ComboboxEmpty, ComboboxInput, ComboboxItem, ComboboxList } from '@/components/ui/combobox';

interface FormComboboxProps<TFieldValues extends FieldValues, TItem> {
  name: Path<TFieldValues>;
  control: Control<TFieldValues>;
  label?: string;
  id?: string;
  items: TItem[] | undefined | null;
  itemValueKey?: keyof TItem;
  itemLabelKey?: keyof TItem;
  placeholder?: string;
  disabled?: boolean;
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  onValueChange?: (value: any) => void;
  fieldClassName?: string;
  inputClassName?: string;
}

export default function FormCombobox<TFieldValues extends FieldValues, TItem>({
  name,
  control,
  label,
  id,
  items,
  itemValueKey = 'id' as keyof TItem,
  itemLabelKey = 'name' as keyof TItem,
  placeholder = 'Select an item',
  disabled,
  onValueChange,
  fieldClassName,
  inputClassName
}: FormComboboxProps<TFieldValues, TItem>) {
  const comboboxId = id ?? `form-combobox-${name.toString()}`;
  const itemsList = items ?? [];

  return (
    <Controller
      name={name}
      control={control}
      render={({ field, fieldState }) => {
        const selectedItem = itemsList.find(item => item[itemValueKey] === field.value);
        const displayValue = selectedItem ? String(selectedItem[itemLabelKey]) : '';

        return (
          <Field data-invalid={fieldState.invalid} className={fieldClassName}>
            {label && <FieldLabel htmlFor={comboboxId}>{label}</FieldLabel>}
            <Combobox
              id={comboboxId}
              items={itemsList}
              value={displayValue}
              onValueChange={value => {
                const selected = itemsList.find(item => String(item[itemLabelKey]) === value);
                const val = selected ? selected[itemValueKey] : null;
                field.onChange(val);
                if (onValueChange) {
                  onValueChange(val);
                }
              }}
              aria-invalid={fieldState.invalid}
            >
              <ComboboxInput placeholder={placeholder} disabled={disabled} className={inputClassName} />
              <ComboboxContent>
                <ComboboxEmpty>No items found.</ComboboxEmpty>
                <ComboboxList>
                  {item => {
                    const val = String(item[itemValueKey]);
                    const labelStr = String(item[itemLabelKey]);
                    return (
                      <ComboboxItem key={val} value={labelStr}>
                        {labelStr}
                      </ComboboxItem>
                    );
                  }}
                </ComboboxList>
              </ComboboxContent>
            </Combobox>
            {fieldState.invalid && <FieldError errors={[fieldState.error]} />}
          </Field>
        );
      }}
    />
  );
}
