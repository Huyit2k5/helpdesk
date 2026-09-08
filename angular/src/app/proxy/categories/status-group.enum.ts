import { mapEnumToOptions } from '@abp/ng.core';

export enum StatusGroup {
  Open = 0,
  InProgress = 1,
  Closed = 2,
}

export const statusGroupOptions = mapEnumToOptions(StatusGroup);
