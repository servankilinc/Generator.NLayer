import { useEffect, useState } from 'react';
import { ShieldCheckIcon, PlusCircleIcon, TrashIcon, SaveIcon } from 'lucide-react';
import axiosHelper from '@/lib/axios-helper';
import { toast } from 'sonner';
import z from 'zod';
import { Controller, useFieldArray, useForm, useWatch } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { Button } from '@/components/ui/button';
import { Dialog, DialogClose, DialogContent, DialogFooter, DialogHeader, DialogTitle, DialogTrigger } from '@/components/ui/dialog';
import { Field, FieldError, FieldLabel } from '@/components/ui/field';
import { Input } from '@/components/ui/input';
import { Separator } from '@/components/ui/separator';
import { Combobox, ComboboxContent, ComboboxEmpty, ComboboxInput, ComboboxItem, ComboboxList } from '@/components/ui/combobox';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import type ValidatorType from '@/models/validatorType/validatorType';
import type ValidatorTypeParam from '@/models/validatorTypeParam/validatorTypeParam';
import { ValidationUpdateSchema } from '@/models/validation/validationUpdateDto';
import type ValidationUpdateDto from '@/models/validation/validationUpdateDto';

const ValidationsFormSchema = z.object({
  validations: z.array(ValidationUpdateSchema)
});

type FormData = z.infer<typeof ValidationsFormSchema>;

export default function DialogValidations({ dtoFieldId }: { dtoFieldId: number }) {
  const [isOpen, setIsOpen] = useState(false);
  const [validatorTypes, setValidatorTypes] = useState<ValidatorType[]>([]);
  const [validatorTypeParamsByIndex, setValidatorTypeParamsByIndex] = useState<Record<number, ValidatorTypeParam[]>>({});

  const form = useForm<FormData>({
    resolver: zodResolver(ValidationsFormSchema),
    defaultValues: { validations: [] }
  });

  const { fields, append, remove } = useFieldArray({
    control: form.control,
    name: 'validations'
  });

  const watchedValidations = useWatch({ control: form.control, name: 'validations' });

  async function fetchValidatorTypes() {
    try {
      const response = await axiosHelper.get<ValidatorType[]>('/validatorType/list');
      setValidatorTypes(response ?? []);
    } catch {
      toast.error('Validator Types could not be loaded!');
    }
  }

  async function fetchValidations() {
    try {
      const response = await axiosHelper.get<ValidationUpdateDto[]>('/validation/list/updateModel', {
        params: { dtoFieldId }
      });
      const validations = response ?? [];
      form.reset({ validations });

      // Her validation için validatorTypeParams yükle
      for (let i = 0; i < validations.length; i++) {
        const v = validations[i];
        if (v.validatorTypeId) {
          await loadValidatorTypeParams(i, v.validatorTypeId);
        }
      }
    } catch {
      toast.error('Validations could not be loaded!');
    }
  }

  async function loadValidatorTypeParams(index: number, validatorTypeId: number) {
    if (!validatorTypeId) {
      setValidatorTypeParamsByIndex(current => ({ ...current, [index]: [] }));
      return;
    }
    try {
      const response = await axiosHelper.get<ValidatorTypeParam[]>('/validatorTypeParam/list', {
        params: { validatorTypeId }
      });
      setValidatorTypeParamsByIndex(current => ({ ...current, [index]: response ?? [] }));
    } catch {
      toast.error('Validator Type Params could not be loaded!');
    }
  }

  useEffect(() => {
    if (isOpen) {
      // eslint-disable-next-line react-hooks/set-state-in-effect
      fetchValidatorTypes();
      fetchValidations();
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [isOpen, dtoFieldId]);

  function handleValidatorTypeChange(index: number, validatorTypeId: number) {
    const selectedType = validatorTypes.find(x => x.id === validatorTypeId);

    // ErrorMessage'ı seçilen ValidatorType'ın description'ından doldur
    form.setValue(`validations.${index}.errorMessage`, selectedType?.description ?? null, { shouldValidate: true });

    // Params'ları sıfırla ve yeniden yükle
    form.setValue(`validations.${index}.validationParams`, null, { shouldValidate: true });
    loadValidatorTypeParams(index, validatorTypeId).then(() => {
      // Yeni paramlar yüklendikten sonra boş param satırları oluştur
      setValidatorTypeParamsByIndex(current => {
        const params = current[index] ?? [];
        if (params.length > 0) {
          form.setValue(
            `validations.${index}.validationParams`,
            params.map(p => ({
              key: p.key,
              validationId: form.getValues(`validations.${index}.validationId`),
              validatorTypeParamId: p.id,
              value: ''
            })),
            { shouldValidate: true }
          );
        }
        return current;
      });
    });
  }

  function handleAppend() {
    append({
      validationId: 0,
      dtoFieldId: dtoFieldId,
      validatorTypeId: 0,
      errorMessage: null,
      validationParams: null
    });
  }

  function handleRemove(index: number) {
    remove(index);
    setValidatorTypeParamsByIndex(current => {
      const next: Record<number, ValidatorTypeParam[]> = {};
      Object.entries(current).forEach(([key, value]) => {
        const numericKey = Number(key);
        if (numericKey < index) {
          next[numericKey] = value;
        } else if (numericKey > index) {
          next[numericKey - 1] = value;
        }
      });
      return next;
    });
  }

  async function onSubmit(data: FormData) {
    try {
      await axiosHelper.post('/validation/list', data.validations);
      toast.success('Validations saved successfully!');
      setIsOpen(false);
    } catch {
      toast.error('Validations could not be saved!');
    }
  }

  return (
    <Dialog open={isOpen} onOpenChange={setIsOpen} modal={false}>
      <DialogTrigger asChild>
        <Button type='button' variant='ghost' size='sm' className='bg-indigo-600 hover:bg-indigo-700 text-white'>
          <ShieldCheckIcon className='size-4' />
        </Button>
      </DialogTrigger>
      <DialogContent className='max-w-5xl max-h-[85vh] overflow-y-auto'>
        <DialogHeader>
          <DialogTitle className='flex items-center gap-2'>
            <ShieldCheckIcon className='size-5 text-indigo-500' />
            Manage Validations
          </DialogTitle>
        </DialogHeader>

        <Separator />

        <form id={`form-validations-${dtoFieldId}`} onSubmit={form.handleSubmit(onSubmit)} className='space-y-4'>
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead className='w-44'>Validator Type</TableHead>
                <TableHead>Error Message</TableHead>
                <TableHead>Params</TableHead>
                <TableHead className='text-right w-16'>Actions</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {fields.map((item, index) => {
                const currentValidatorTypeId = watchedValidations?.[index]?.validatorTypeId ?? 0;
                const params = validatorTypeParamsByIndex[index] ?? [];

                return (
                  <TableRow key={item.id} className='align-top'>
                    {/* Validator Type */}
                    <TableCell className='font-medium'>
                      <Controller
                        name={`validations.${index}.validatorTypeId`}
                        control={form.control}
                        render={({ field, fieldState }) => (
                          <Field data-invalid={fieldState.invalid} className='w-40'>
                            <Combobox
                              items={validatorTypes}
                              value={validatorTypes.find(x => x.id === field.value)?.name ?? ''}
                              onValueChange={value => {
                                const selected = validatorTypes.find(x => x.name === value);
                                if (selected) {
                                  field.onChange(selected.id);
                                  handleValidatorTypeChange(index, selected.id);
                                }
                              }}
                              aria-invalid={fieldState.invalid}>
                              <ComboboxInput placeholder='Select type' />
                              <ComboboxContent>
                                <ComboboxEmpty>No items found.</ComboboxEmpty>
                                <ComboboxList>
                                  {vt => (
                                    <ComboboxItem key={vt.id} value={vt.name}>
                                      {vt.name}
                                    </ComboboxItem>
                                  )}
                                </ComboboxList>
                              </ComboboxContent>
                            </Combobox>
                            {fieldState.invalid && <FieldError errors={[fieldState.error]} />}
                          </Field>
                        )}
                      />
                    </TableCell>

                    {/* Error Message */}
                    <TableCell className='font-medium'>
                      <Controller
                        name={`validations.${index}.errorMessage`}
                        control={form.control}
                        render={({ field, fieldState }) => (
                          <Field data-invalid={fieldState.invalid} className='min-w-48'>
                            <Input
                              {...field}
                              value={field.value ?? ''}
                              aria-invalid={fieldState.invalid}
                              placeholder='Error message'
                              autoComplete='off'
                              className='w-full'
                            />
                            {fieldState.invalid && <FieldError errors={[fieldState.error]} />}
                          </Field>
                        )}
                      />
                    </TableCell>

                    {/* Params */}
                    <TableCell>
                      {currentValidatorTypeId !== 0 && params.length > 0 ? (
                        <div className='space-y-2'>
                          {params.map((param, paramIndex) => (
                            <div key={param.id} className='flex items-center gap-2'>
                              <span className='text-xs text-muted-foreground w-20 shrink-0'>
                                <FieldLabel>{param.key}</FieldLabel>
                              </span>
                              <Controller
                                name={`validations.${index}.validationParams.${paramIndex}.value`}
                                control={form.control}
                                render={({ field, fieldState }) => (
                                  <Field data-invalid={fieldState.invalid} className='flex-1'>
                                    <Input
                                      {...field}
                                      value={field.value ?? ''}
                                      aria-invalid={fieldState.invalid}
                                      placeholder={`${param.key} value`}
                                      autoComplete='off'
                                      className='w-full'
                                    />
                                    {fieldState.invalid && <FieldError errors={[fieldState.error]} />}
                                  </Field>
                                )}
                              />
                            </div>
                          ))}
                        </div>
                      ) : currentValidatorTypeId !== 0 ? (
                        <span className='text-xs text-muted-foreground'>No params</span>
                      ) : null}
                    </TableCell>

                    {/* Remove */}
                    <TableCell className='text-right'>
                      <Button type='button' variant='destructive' size='icon-sm' onClick={() => handleRemove(index)}>
                        <TrashIcon className='size-4' color='red' />
                      </Button>
                    </TableCell>
                  </TableRow>
                );
              })}
              <TableRow>
                <TableCell colSpan={4} className='text-center'>
                  <Button type='button' size='sm' variant='ghost' onClick={handleAppend} className='my-1 bg-gray-700'>
                    <PlusCircleIcon className='mr-2' /> Add Validation
                  </Button>
                </TableCell>
              </TableRow>
            </TableBody>
          </Table>
        </form>

        <DialogFooter>
          <DialogClose asChild>
            <Button type='button' variant='outline'>
              Cancel
            </Button>
          </DialogClose>
          <Button type='submit' form={`form-validations-${dtoFieldId}`} className='bg-indigo-600 hover:bg-indigo-700 text-white'>
            <SaveIcon className='mr-1' /> Save Validations
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
