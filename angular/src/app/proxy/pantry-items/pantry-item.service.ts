import type { CreatePantryItemDto, GetPantryItemListInput, PantryItemDto, UpdatePantryItemDto } from './models';
import { RestService, Rest } from '@abp/ng.core';
import type { PagedResultDto } from '@abp/ng.core';
import { Injectable, inject } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class PantryItemService {
  private restService = inject(RestService);
  apiName = 'Default';
  

  create = (input: CreatePantryItemDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, PantryItemDto>({
      method: 'POST',
      headers: { Accept: 'application/json' },
      url: '/api/app/pantry-item',
      body: input,
    },
    { apiName: this.apiName,...config });
  

  delete = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'DELETE',
      url: `/api/app/pantry-item/${id}`,
    },
    { apiName: this.apiName,...config });
  

  get = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, PantryItemDto>({
      method: 'GET',
      headers: { Accept: 'application/json' },
      url: `/api/app/pantry-item/${id}`,
    },
    { apiName: this.apiName,...config });
  

  getList = (input: GetPantryItemListInput, config?: Partial<Rest.Config>) =>
    this.restService.request<any, PagedResultDto<PantryItemDto>>({
      method: 'GET',
      headers: { Accept: 'application/json' },
      url: '/api/app/pantry-item',
      params: { includeConsumed: input.includeConsumed, sorting: input.sorting, skipCount: input.skipCount, maxResultCount: input.maxResultCount },
    },
    { apiName: this.apiName,...config });
  

  markAsConsumed = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, PantryItemDto>({
      method: 'POST',
      headers: { Accept: 'application/json' },
      url: `/api/app/pantry-item/${id}/mark-as-consumed`,
    },
    { apiName: this.apiName,...config });
  

  update = (id: string, input: UpdatePantryItemDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, PantryItemDto>({
      method: 'PUT',
      headers: { Accept: 'application/json' },
      url: `/api/app/pantry-item/${id}`,
      body: input,
    },
    { apiName: this.apiName,...config });
}