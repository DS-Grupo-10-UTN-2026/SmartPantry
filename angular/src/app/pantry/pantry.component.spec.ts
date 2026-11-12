import { beforeEach, describe, expect, it, vi } from 'vitest';
import { DatePipe } from '@angular/common';
import { Pipe, PipeTransform } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ReactiveFormsModule } from '@angular/forms';
import { LIST_QUERY_DEBOUNCE_TIME, LocalizationService } from '@abp/ng.core';
import { Confirmation, ConfirmationService, ToasterService } from '@abp/ng.theme.shared';
import { Subject, of, throwError } from 'rxjs';
import { PantryComponent } from './pantry.component';
import { PantryItemDto, PantryItemService, PantryItemStatus } from '../proxy/pantry-items';

// Reemplaza al pipe real: devuelve la clave pedida, así se prueba qué texto solicita la pantalla.
@Pipe({ name: 'abpLocalization' })
class LocalizationPipeStub implements PipeTransform {
  transform(key: string, ...params: string[]): string {
    return params.length ? `${key}[${params.join(',')}]` : key;
  }
}

const item = (overrides: Partial<PantryItemDto> = {}): PantryItemDto => ({
  id: 'id-1',
  productId: 'product-1',
  productName: 'Arroz',
  status: PantryItemStatus.Active,
  expirationDate: '2026-11-04',
  quantity: 2,
  unit: 'kg',
  ...overrides,
});

const page = (items: PantryItemDto[], totalCount = items.length) => ({ items, totalCount });

describe('PantryComponent', () => {
  const pantryService = {
    getList: vi.fn(),
    update: vi.fn(),
    markAsConsumed: vi.fn(),
    delete: vi.fn(),
  };
  const confirmationService = { warn: vi.fn() };
  const toasterService = { success: vi.fn(), error: vi.fn() };

  let fixture: ComponentFixture<PantryComponent>;
  let component: PantryComponent;

  const element = () => fixture.nativeElement as HTMLElement;
  const text = () => element().textContent ?? '';
  const rows = () => Array.from(element().querySelectorAll('tbody tr'));
  const cells = (row: Element) =>
    Array.from(row.querySelectorAll('td')).map(cell => (cell.textContent ?? '').trim());

  // getList se invoca con (entrada, { skipHandleError: true }); se compara la entrada.
  const expectLastListCall = (input: unknown) =>
    expect(pantryService.getList).toHaveBeenLastCalledWith(input, { skipHandleError: true });

  // Se crea el componente después de configurar las respuestas del servicio simulado.
  const create = () => {
    fixture = TestBed.createComponent(PantryComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  };

  beforeEach(async () => {
    vi.resetAllMocks();
    pantryService.getList.mockReturnValue(of(page([item()])));
    pantryService.update.mockReturnValue(of(undefined));
    pantryService.markAsConsumed.mockReturnValue(of(undefined));
    pantryService.delete.mockReturnValue(of(undefined));
    confirmationService.warn.mockReturnValue(of(Confirmation.Status.confirm));

    TestBed.overrideComponent(PantryComponent, {
      set: { imports: [DatePipe, ReactiveFormsModule, LocalizationPipeStub] },
    });

    await TestBed.configureTestingModule({
      imports: [PantryComponent],
      providers: [
        { provide: PantryItemService, useValue: pantryService },
        { provide: ConfirmationService, useValue: confirmationService },
        { provide: ToasterService, useValue: toasterService },
        { provide: LocalizationService, useValue: { instant: (key: string) => key } },
        { provide: LIST_QUERY_DEBOUNCE_TIME, useValue: 0 },
      ],
    }).compileComponents();
  });

  // ---- Los cuatro estados ----

  it('muestra el estado de carga mientras la consulta no responde', () => {
    pantryService.getList.mockReturnValue(new Subject());

    create();

    expect(text()).toContain('::Pantry:Loading');
    expect(element().querySelector('table')).toBeNull();
    expect(text()).not.toContain('::Pantry:Empty');
  });

  it('muestra el error con un botón para reintentar', () => {
    pantryService.getList
      .mockReturnValueOnce(throwError(() => new Error('falla')))
      .mockReturnValue(of(page([item()])));

    create();

    expect(text()).toContain('::Pantry:LoadError');
    expect(element().querySelector('table')).toBeNull();

    element().querySelector<HTMLButtonElement>('.alert button')!.click();
    fixture.detectChanges();

    expect(pantryService.getList).toHaveBeenCalledTimes(2);
    expect(element().querySelector('table')).not.toBeNull();
    expect(text()).not.toContain('::Pantry:LoadError');
  });

  it('muestra el mensaje de despensa vacía', () => {
    pantryService.getList.mockReturnValue(of(page([])));

    create();

    expect(text()).toContain('::Pantry:Empty');
    expect(element().querySelector('table')).toBeNull();
  });

  it('muestra una fila por cada ítem con sus datos', () => {
    pantryService.getList.mockReturnValue(
      of(page([item(), item({ id: 'id-2', productName: 'Fideos', quantity: 3, unit: 'u' })])),
    );

    create();

    expect(rows()).toHaveLength(2);
    expect(cells(rows()[0])[0]).toBe('Arroz');
    expect(cells(rows()[1])[0]).toBe('Fideos');
    expect(cells(rows()[1])[2]).toBe('3');
    expect(cells(rows()[1])[3]).toBe('u');
  });

  // ---- Datos faltantes (RF-09) ----

  it('muestra "Sin dato" cuando faltan datos, sin inventar valores', () => {
    pantryService.getList.mockReturnValue(
      of(
        page([
          item({
            productName: undefined,
            expirationDate: undefined,
            quantity: undefined,
            unit: undefined,
          }),
        ]),
      ),
    );

    create();

    const [name, expiration, quantity, unit] = cells(rows()[0]);
    expect(name).toBe('::Pantry:NoData');
    expect(expiration).toBe('::Pantry:NoData');
    expect(quantity).toBe('::Pantry:NoData');
    expect(unit).toBe('::Pantry:NoData');
  });

  it('muestra la cantidad cero como 0 y no como dato faltante', () => {
    pantryService.getList.mockReturnValue(of(page([item({ quantity: 0 })])));

    create();

    expect(cells(rows()[0])[2]).toBe('0');
  });

  it('los ítems consumidos solo ofrecen la acción de eliminar', () => {
    pantryService.getList.mockReturnValue(
      of(page([item(), item({ id: 'id-2', status: PantryItemStatus.Consumed })])),
    );

    create();

    expect(rows()[0].querySelectorAll('td:last-child button')).toHaveLength(3);
    expect(rows()[1].querySelectorAll('td:last-child button')).toHaveLength(1);
    expect(rows()[1].querySelector('button.btn-outline-danger')).not.toBeNull();
  });

  // ---- Filtro, orden y paginación ----

  it('no pide los consumidos por defecto y los pide al activar el interruptor', () => {
    create();

    expectLastListCall(
      expect.objectContaining({ includeConsumed: false, skipCount: 0, maxResultCount: 10 }),
    );

    const toggle = element().querySelector<HTMLInputElement>('#include-consumed')!;
    toggle.checked = true;
    toggle.dispatchEvent(new Event('change'));
    fixture.detectChanges();

    expectLastListCall(
      expect.objectContaining({ includeConsumed: true, skipCount: 0 }),
    );
  });

  it('ordena por vencimiento de forma ascendente y alterna al elegir la misma columna', () => {
    create();

    expectLastListCall(
      expect.objectContaining({ sorting: 'ExpirationDate asc' }),
    );

    component.sortBy('ProductName');
    expectLastListCall(
      expect.objectContaining({ sorting: 'ProductName asc', skipCount: 0 }),
    );

    component.sortBy('ProductName');
    expectLastListCall(
      expect.objectContaining({ sorting: 'ProductName desc' }),
    );
  });

  it('pagina de a diez ítems e ignora las páginas inexistentes', () => {
    pantryService.getList.mockReturnValue(of(page([item()], 25)));

    create();

    expect(component.totalPages()).toBe(3);

    component.goToPage(2);
    expectLastListCall(
      expect.objectContaining({ skipCount: 10, maxResultCount: 10 }),
    );

    const calls = pantryService.getList.mock.calls.length;
    component.goToPage(5);
    component.goToPage(0);
    expect(pantryService.getList).toHaveBeenCalledTimes(calls);
    expect(component.page()).toBe(2);
  });

  // ---- Editar ----

  it('abre el panel de edición con los valores del ítem y se cierra al cancelar', () => {
    create();

    component.startEdit(item());
    fixture.detectChanges();

    expect(element().querySelector('form')).not.toBeNull();
    expect(component.form.value).toEqual({
      expirationDate: '2026-11-04',
      quantity: 2,
      unit: 'kg',
    });

    component.cancelEdit();
    fixture.detectChanges();

    expect(element().querySelector('form')).toBeNull();
  });

  it('guarda los cambios con la unidad recortada, avisa y recarga la lista', () => {
    create();
    component.startEdit(item());
    component.form.setValue({ expirationDate: '2026-12-01', quantity: 5, unit: '  g ' });

    component.save();

    expect(pantryService.update).toHaveBeenCalledWith(
      'id-1',
      { expirationDate: '2026-12-01', quantity: 5, unit: 'g' },
      { skipHandleError: true },
    );
    expect(toasterService.success).toHaveBeenCalledWith('::Pantry:Updated');
    expect(component.editing()).toBeNull();
    expect(pantryService.getList).toHaveBeenCalledTimes(2);
  });

  it('envía los campos vacíos como ausentes (sin dato)', () => {
    create();
    component.startEdit(item());
    component.form.setValue({ expirationDate: '', quantity: null, unit: '' });

    component.save();

    const sent = pantryService.update.mock.calls[0][1];
    expect(sent.expirationDate).toBeUndefined();
    expect(sent.quantity).toBeUndefined();
    expect(sent.unit).toBeUndefined();
  });

  it('no guarda si la cantidad es negativa o la unidad es demasiado larga', () => {
    create();
    component.startEdit(item());

    component.form.setValue({ expirationDate: '2026-12-01', quantity: -1, unit: 'kg' });
    fixture.detectChanges();
    expect(component.form.controls.quantity.errors?.['min']).toBeTruthy();
    expect(text()).toContain('::Pantry:Validation:QuantityMin');
    component.save();

    component.form.setValue({
      expirationDate: '2026-12-01',
      quantity: 1,
      unit: 'x'.repeat(component.unitMaxLength + 1),
    });
    fixture.detectChanges();
    expect(component.form.controls.unit.errors?.['maxlength']).toBeTruthy();
    expect(text()).toContain('::Pantry:Validation:UnitMaxLength');
    component.save();

    expect(pantryService.update).not.toHaveBeenCalled();
  });

  it('no guarda si la fecha de vencimiento no es válida y muestra el mensaje', () => {
    create();
    component.startEdit(item());

    for (const invalid of ['2026-02-30', '2026-13-01', '275760-01-01', '1800-01-01']) {
      component.form.controls.expirationDate.setValue(invalid);
      fixture.detectChanges();
      expect(component.form.controls.expirationDate.errors?.['invalidDate']).toBeTruthy();
      expect(text()).toContain('::Pantry:Validation:ExpirationDateInvalid');
      component.save();
    }

    expect(pantryService.update).not.toHaveBeenCalled();
  });

  it('acepta la fecha vacía (sin dato) y las fechas reales', () => {
    create();
    component.startEdit(item());

    for (const valid of ['', '2026-12-31', '2028-02-29']) {
      component.form.controls.expirationDate.setValue(valid);
      expect(component.form.controls.expirationDate.errors).toBeNull();
    }
  });

  it('no guarda mientras la fecha esté incompleta y guarda cuando se completa', () => {
    create();
    component.startEdit(item());

    component.onExpirationDateInput({ target: { validity: { badInput: true } } } as unknown as Event);
    fixture.detectChanges();
    expect(text()).toContain('::Pantry:Validation:ExpirationDateInvalid');
    component.save();
    expect(pantryService.update).not.toHaveBeenCalled();

    component.onExpirationDateInput({ target: { validity: { badInput: false } } } as unknown as Event);
    component.save();
    expect(pantryService.update).toHaveBeenCalledTimes(1);
  });

  it('detecta la fecha incompleta al guardar aunque el campo no haya emitido ningún evento', () => {
    create();
    component.startEdit(item());
    fixture.detectChanges();

    // Chrome no emite eventos al tipear solo una parte de la fecha: se simula validity.badInput.
    const input = element().querySelector<HTMLInputElement>('#edit-expiration-date')!;
    Object.defineProperty(input, 'validity', { value: { badInput: true }, configurable: true });

    component.save();
    fixture.detectChanges();

    expect(pantryService.update).not.toHaveBeenCalled();
    expect(component.dateIncomplete()).toBe(true);
    expect(text()).toContain('::Pantry:Validation:ExpirationDateInvalid');
  });

  // ---- Marcar como consumido ----

  it('marca un ítem como consumido, avisa y recarga la lista', () => {
    create();

    component.markAsConsumed(item());

    expect(pantryService.markAsConsumed).toHaveBeenCalledWith('id-1', { skipHandleError: true });
    expect(toasterService.success).toHaveBeenCalledWith('::Pantry:ConsumedMessage');
    expect(pantryService.getList).toHaveBeenCalledTimes(2);
    expect(component.busyId()).toBeNull();
  });

  it('avisa del error si la operación falla', () => {
    pantryService.markAsConsumed.mockReturnValue(throwError(() => new Error('falla')));
    create();

    component.markAsConsumed(item());

    expect(toasterService.error).toHaveBeenCalledWith('::Pantry:ActionError');
    expect(toasterService.success).not.toHaveBeenCalled();
    expect(component.busyId()).toBeNull();
  });

  // ---- Eliminar ----

  it('elimina el ítem solo si el usuario confirma', () => {
    create();

    component.deleteItem(item());

    expect(confirmationService.warn).toHaveBeenCalledWith(
      '::Pantry:DeleteConfirmation',
      '::Pantry:DeleteTitle',
      { messageLocalizationParams: ['Arroz'] },
    );
    expect(pantryService.delete).toHaveBeenCalledWith('id-1', { skipHandleError: true });
    expect(toasterService.success).toHaveBeenCalledWith('::Pantry:Deleted');
    expect(pantryService.getList).toHaveBeenCalledTimes(2);
  });

  it('no elimina nada si el usuario cancela la confirmación', () => {
    confirmationService.warn.mockReturnValue(of(Confirmation.Status.reject));
    create();

    component.deleteItem(item());

    expect(pantryService.delete).not.toHaveBeenCalled();
    expect(toasterService.success).not.toHaveBeenCalled();
  });

  it('vuelve a la página anterior si se elimina el único ítem de la última página', () => {
    pantryService.getList.mockReturnValue(of(page([item()], 11)));
    create();
    component.goToPage(2);
    expect(component.page()).toBe(2);

    component.deleteItem(item());

    expect(component.page()).toBe(1);
  });
});
