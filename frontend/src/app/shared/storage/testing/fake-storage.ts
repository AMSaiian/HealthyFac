export class FakeStorage implements Storage {
  readonly items = new Map<string, string>();

  failOnGet = false;
  failOnSet = false;

  get length(): number {
    return this.items.size;
  }

  clear(): void {
    this.items.clear();
  }

  getItem(key: string): string | null {
    if (this.failOnGet) {
      throw new Error('getItem failed');
    }
    return this.items.get(key) ?? null;
  }

  key(index: number): string | null {
    return [...this.items.keys()][index] ?? null;
  }

  removeItem(key: string): void {
    this.items.delete(key);
  }

  setItem(key: string, value: string): void {
    if (this.failOnSet) {
      throw new Error('setItem failed');
    }
    this.items.set(key, value);
  }
}
